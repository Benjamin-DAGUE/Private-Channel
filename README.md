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
- [ ] Localization (FR)
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

# Contributions
This is my first open source project and I'm note used to it yet but feel free to contact me if you want to help or support this project!
