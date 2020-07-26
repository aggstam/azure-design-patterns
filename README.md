# azure-design-patterns
This project compines Gatekeeper, Valet Key and Static Content Hosting patterns using Azure Storage emulator.

The basic functionality of the project is to provide remote storage actions for a picture repository site, along with user database and share links for the images.

Two REST APIs exist, one for the Gatekeeper(Gateway) and one for the Backend.
Gatekeeper API exposes the public URLs of the application, performs requests validation and forward each valid request to Backend to be processed.
BackEnd API execute request processing and have access to Database for user authentication/creation and Azure Storage for file actions.
