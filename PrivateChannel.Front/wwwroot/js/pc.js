window.generateKeyPair = async function () {
    let keyPair = await window.crypto.subtle.generateKey(
        {
            name: "RSA-OAEP",
            modulusLength: 2048,
            publicExponent: new Uint8Array([1, 0, 1]),
            hash: "SHA-256",
        },
        true,
        ["encrypt", "decrypt"]
    );

    let publicKey = await window.crypto.subtle.exportKey(
        "spki",
        keyPair.publicKey
    );

    let privateKey = await window.crypto.subtle.exportKey(
        "pkcs8",
        keyPair.privateKey
    );

    return {
        publicKey: Array.from(new Uint8Array(publicKey)),
        privateKey: Array.from(new Uint8Array(privateKey))
    };
}

window.generateAndEncryptSymmetricKey = async function (publicKeyAsIntArray) {
    // Convertir le tableau d'entiers en tableau d'octets
    let publicKeyAsByteArray = new Uint8Array(publicKeyAsIntArray);

    // Importer la clé publique
    let publicKey = await window.crypto.subtle.importKey(
        "spki",
        publicKeyAsByteArray,
        { name: "RSA-OAEP", hash: "SHA-256" },
        true,
        ["encrypt"]
    );

    // Générer une clé symétrique
    let symmetricKey = await window.crypto.subtle.generateKey(
        { name: "AES-GCM", length: 256 }, // Algorithm
        true,                             // Extractable
        ["encrypt", "decrypt"]            // Key usages
    );

    // Exporter la clé symétrique sous forme brute
    let rawSymmetricKey = await window.crypto.subtle.exportKey(
        "raw",
        symmetricKey
    );

    // Chiffrer la clé symétrique avec la clé publique
    let encryptedSymmetricKey = await window.crypto.subtle.encrypt(
        { name: "RSA-OAEP" },
        publicKey,
        rawSymmetricKey
    );

    return {
        raw: Array.from(new Uint8Array(rawSymmetricKey)),
        encrypted: Array.from(new Uint8Array(encryptedSymmetricKey)),
    }
}

window.encryptWithSymmetricKey = async function (inputString, symmetricKeyAsIntArray) {
    // Convertir le tableau d'entiers en tableau d'octets
    let symmetricKeyAsByteArray = new Uint8Array(symmetricKeyAsIntArray);

    // Importer la clé symétrique
    let symmetricKey = await window.crypto.subtle.importKey(
        "raw",
        symmetricKeyAsByteArray,
        { name: "AES-GCM", length: 256 },
        false,
        ["encrypt", "decrypt"]
    );

    // Générer un vecteur d'initialisation aléatoire
    let iv = window.crypto.getRandomValues(new Uint8Array(12));

    // Convertir la chaîne d'entrée en tableau d'octets
    let inputBytes = new TextEncoder().encode(inputString);

    // Chiffrer la chaîne d'entrée avec la clé symétrique
    let encryptedBytes = await window.crypto.subtle.encrypt(
        { name: "AES-GCM", iv: iv },
        symmetricKey,
        inputBytes
    );

    return {
        iv: Array.from(iv),
        encryptedData: Array.from(new Uint8Array(encryptedBytes))
    };
}

window.decryptWithPrivateKeyAndSymmetricKey = async function (encryptedDataAsIntArray, ivAsIntArray, encryptedSymmetricKeyAsIntArray, privateKeyAsIntArray) {

    try {
        // Convertir le tableau d'entiers en tableau d'octets
        let encryptedDataAsByteArray = new Uint8Array(encryptedDataAsIntArray);
        let ivAsByteArray = new Uint8Array(ivAsIntArray);
        let encryptedSymmetricKeyAsByteArray = new Uint8Array(encryptedSymmetricKeyAsIntArray);
        let privateKeyAsByteArray = new Uint8Array(privateKeyAsIntArray);

        // Importer la clé privée
        let privateKey = await window.crypto.subtle.importKey(
            "pkcs8",
            privateKeyAsByteArray,
            { name: "RSA-OAEP", hash: "SHA-256" },
            false,
            ["decrypt"]
        );

        // Déchiffrer la clé symétrique avec la clé privée
        let symmetricKeyAsByteArray = await window.crypto.subtle.decrypt(
            { name: "RSA-OAEP" },
            privateKey,
            encryptedSymmetricKeyAsByteArray
        );

        // Importer la clé symétrique déchiffrée
        let symmetricKey = await window.crypto.subtle.importKey(
            "raw",
            symmetricKeyAsByteArray,
            { name: "AES-GCM", length: 256 },
            false,
            ["encrypt", "decrypt"]
        );

        // Déchiffrer les données chiffrées avec la clé symétrique et l'IV
        let decryptedBytes = await window.crypto.subtle.decrypt(
            { name: "AES-GCM", iv: ivAsByteArray },
            symmetricKey,
            encryptedDataAsByteArray
        );

        // Convertir les octets déchiffrés en chaîne
        let decryptedString = new TextDecoder().decode(decryptedBytes);

        return decryptedString;
    } catch (error) {
        console.error(error);
    }


    return "";
}

window.encryptWithPassword = async function (text, password) {
    const encoder = new TextEncoder();
    const data = encoder.encode(text);

    const passwordKey = await window.crypto.subtle.importKey(
        "raw",
        encoder.encode(password),
        { name: "PBKDF2" },
        false,
        ["deriveBits", "deriveKey"]
    );

    const salt = window.crypto.getRandomValues(new Uint8Array(16));
    const iv = window.crypto.getRandomValues(new Uint8Array(12));

    const key = await window.crypto.subtle.deriveKey(
        {
            name: "PBKDF2",
            salt: salt,
            iterations: 5000,
            hash: "SHA-256"
        },
        passwordKey,
        { name: "AES-GCM", length: 256 },
        true,
        ["encrypt", "decrypt"]
    );

    const cipherTextWithAuthTag = await window.crypto.subtle.encrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        key,
        data
    );

    const cipherText = cipherTextWithAuthTag.slice(0, cipherTextWithAuthTag.byteLength - 16);
    const authTag = cipherTextWithAuthTag.slice(cipherTextWithAuthTag.byteLength - 16);

    return {
        cipherText: Array.from(new Uint8Array(cipherText)),
        authTag: Array.from(new Uint8Array(authTag)),
        iv: Array.from(iv),
        salt: Array.from(salt)
    };
}