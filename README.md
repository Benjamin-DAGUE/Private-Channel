# Private-Channel
[private-channel.com](https://private-channel.com) : Private, encrypted channels and notes for all your confidential conversations.

This software is under [MIT license](./LICENSE.txt). This license is clear and short, please read it before use.

> ⚠️ **WARNING**
>
> As stated in license, this software is provided "as is" without any warranty.

I started this project for educational purpose and for personal needs.

[private-channel.com](https://private-channel.com) is my own public instance of this project. You can use ite freely, at least for now. Front-end Web App is hosted on OVH Cloud and Back-end is hosted with Azure App Service.

This software is in early development stage and is subject to change a lot.

I'll write some doc on how to host your own instance once Core functionality will be implemented.

# Core FX
- [x] End to end encrypted channels for live chat messaging
- [x] End to end encrypted note
- [ ] Databse persistence for note
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

## Front-end
the Web App is in dotnet Blazor Web Assembly. MudBlazor is used as UI component library. Encryption is done with JavaScript using only browser native crypto library.

Front-end is hosted as a static website and is serverless.

## Back-end
PrivateChannel server is in dotnet Asp NET Core and use gRPC Web for communication with Front-end. A custom Ban list is implemented to prevent brute force or DDOS. HTTPS is used to secure communication with Front-end.

## Database
For now it's not implemented (in memory storage) but I'll use EF Core.

## Note
Note are password encrypted client side.


## Channel

# Contributions
This is my first open source project and I'm note used to it yet but feel free to contact me if you want to help or support this project!
