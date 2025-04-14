# Azure B2C Security App

A very minimal, simple, no frills ASP.NET application which can implement some basic checks for Azure B2C tenants in your organisation.

All it does is use the Graph API to check how many objects there are in the tenant (1 object ~=~ 1 user) and if the limit is reached, it will block sign ups. This application takes advantage of the API Connector functionality within Azure B2C.

# Features

- Customizable Graph API object limit checks
- Uses StopForumSpam domain blocklist to block known bad actor domains
- Automatically updates StopForumSpam domain blocklist
- Very easy to audit. Why should you trust me? Trust yourself instead.
- Minimal maintenance overall. Just patch the runtime and keep the 2 libraries updated.
- No nonsense. If this has even a hint of nonsense anywhere, it is a bug. File an issue.

# Azure resources shopping list

- Azure KeyVault
   - Must be network accessible by the Azure App Service
   - Must grant Azure App Service the Azure RBAC role of "Key Vault Secrets User" on the Azure KeyVault
   - Azure KeyVault will store the Microsoft Graph API secret and the hCaptcha secret
- Azure App Service
   - Must have outbound network access to the hCaptcha API and the Microsoft Graph API
   - Must have the source code from this repo deployed to it

# Environment Variables

TODO!

# Disclaimer

This repository is not endorsed by my employer, organisation, clients, anyone, anything or any entity in any way, shape or form. This is released on the internet as a convenience only. Usage of this application may induce lucid states where the user gains the ability to implement and plot graph functions which spell out "Microsoft Graph API". No refunds, no "I can't shake my inherent urge to write a paper on making graph functions that spell out 'Microsoft Graph API', please make it stop" support here. 
