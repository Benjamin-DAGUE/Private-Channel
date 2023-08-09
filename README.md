# Private-Channel
[private-channel.com](https://private-channel.com) : Private, encrypted channels and notes for all your confidential conversations.

This software is under [MIT license](./LICENSE.txt). This license is clear and short, please read it before use.

> ⚠️ **WARNING**
>
> As stated in license, this software is provided "as is" without any warranty.

I started this project for educational purpose and for personal needs.

[private-channel.com](https://private-channel.com) is my own public instance of this project. You can use ite freely, at least for now. 
Frontend Web App is hosted on OVH Cloud and Back-end is hosted with Azure App Service.
This software is in early development stage and is subject to change a lot.
I'll write some doc on how to host your own instance once Core functionality will be implemented.

# Core FX
- [x] End to end encrypted channels for live chat messaging
- [x] End to end encrypted note
- [X] Databse persistence for note
- [X] Localization (FR)
- [ ] Security code review and documentation

# Extended FX
- [ ] Other Localizations
- [ ] Securely embed files in note
- [ ] Securely share files in channel
- [ ] Unlock attempts counter for note
- [ ] Mail notification on unlock note event
- [ ] User authentication capability
- [ ] Group channel for live messaging
- [ ] PWA installation
- [ ] Packaging
- [ ] 2FA for unlocking note

# How it works

## Frontend
the Web App is in dotnet Blazor Web Assembly. MudBlazor is used as UI component library. Encryption is done with JavaScript using only browser native crypto library.

Front-end is hosted as a static website and is serverless.

## Backend
PrivateChannel server is in dotnet Asp NET Core and use gRPC Web for communication with frontend. A custom Ban list is implemented to prevent brute force or DDOS. HTTPS is used to secure communication with Front-end.

## Database
Databse is using Azure SQL Server database.

## Note
Messages within notes are encrypted client-side using JavaScript's native Crypto API and a password.
If the user doesn't provide one, a 16-character password is generated randomly.
This password undergoes a transformation through PBKDF2 into a SHA-256 key, leveraging a random salt and 5,000 iterations.
Subsequently, with a random IV, the key encrypts the text message using AES-GCM.
This encryption method not only secures the data but also produces an authentication tag, ensuring data integrity.

When transmitting the cipher text, authentication tag, IV, salt, and selected retention delay, HTTPS is employed to ensure transmission security.
On the server side, a random 16-byte ID for the note is crafted using `System.Security.Cryptography.RandomNumberGenerator`.
All pertinent data is subsequently stored within an SQL Server database.
In return, the server dispatches the NoteId, which the client harnesses to construct the note URI.
When a user seeks to access the note, they provide both the password and the note ID via HTTPS.
The server then attempts decryption of the note, forwarding the resultant message to the client.
We opted for server-side decryption as a safeguard against brute force attacks, aided by a stringent ban list, and to facilitate the deletion of notes once their predetermined retention period expires.

## Channel
When a user creates a new channel, the server receives a request.
In response, the server generates a random 16-byte ID for the note using `System.Security.Cryptography.RandomNumberGenerator`.
After creating the ID, the user is automatically redirected to the channel's page.

Before initiating a connection, an asymmetric key pair is generated client side using RSA-OAEP.
This key generation employs a 2048-bit modulus length and the SHA-256 hashing algorithm.
While the private key remains securely stored in memory, the public key is sent to the server when the user tries to connect to the channel.

The connection process involves a gRPC stream, which notifies users when another peer connects or disconnects.
This system facilitates the exchange of public keys between peers.

Every peer that connects generates a symmetric key known as the session key.
This key is crafted using the AES-GCM algorithm and is encrypted with the public key of the recipient. This ensures that only the intended peer can decrypt the session key with their private key, making the communication secure.

When sending a message, a peer encrypts the content using the session key.
This encryption is combined with a unique, one-time-use 12-bit Initialization Vector (IV).
The resulting encrypted message and the IV are then transmitted to the other peer.

Upon joining a channel, peers immediately start receiving messages through a gRPC stream.
When a message arrives, the peer first checks if a new session key accompanies it.
If such a key is present, the peer decrypts this key using their private key, setting it as the active session key for further communications.
Following this, the actual message is decrypted using the current session key.

Channels use real end to end encryption and all cryptographic workload are done on peers devices.


# Contributions
This is my first open source project and I'm note used to it yet but feel free to contact me if you want to help or support this project!