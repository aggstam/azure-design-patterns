# azure-design-patterns
This project compines Gatekeeper, Valet Key and Static Content Hosting patterns using Azure Storage emulator.

The basic functionality of the project is to provide remote storage actions for a picture repository site, along with user database and share links for the images.

Two REST APIs exist, one for the Gatekeeper(Gateway) and one for the Backend.
<br>
Gatekeeper API exposes the public URLs of the application, performs requests validation and forward each valid request to Backend to be processed.
<br>
BackEnd API execute request processing and have access to Database for user authentication/creation and Azure Storage for file actions.

## Prerequisites
- ASP.NET Core(C#) v3.1
- MongoDB v4.2.8
- Microsoft Azure Storage Emulator v5.10

Place `secrets.json` under UserSercrets folder:
```text
C:\Users\<user>\AppData\Roaming\Microsoft\UserSecrets\<app_id>
```

Initialize MongoDB with project data folder:
```shell
$ mongod --dbpath <path_to_folder>\azure-design-patterns\BackEnd\database\UsersData
```

To generate users execute the following commands in MongoDB:
```C#
db.Users.insertMany(
    [
        {'FirstName':'','LastName':'','Email':'','Username':'gatekeeper','Password':'gatekeeper'},
        {'FirstName':'Aggelos','LastName':'Stamatiou','Email':'aggelos@gmail.com','Username':'aggelos','Password':'aggelos'},
        {'FirstName':'Giorgos','LastName':'Weider','Email':'giorgos@gmail.com','Username':'giorgos','Password':'giorgos'},
        {'FirstName':'Stavros','LastName':'Laios','Email':'stavros@gmail.com','Username':'stavros','Password':'stavros'}
    ]
)
```

## Runtime notes
- Local GateKeeper url: https://localhost:44373/gatekeeper/
- Local BackEnd url: https://localhost:44373/gatekeeper/
- Local Azure Storage Emulator url: http://localhost:10000/devstoreaccount1/static-content/

Both GateKeeper and BackEnd use HTTPS.
<br>
GateKeeper endpoints are public, while BackEnd are accessible only in local network.
<br>
Base Authentication is used on GateKeeper for User Authentication.
<br>
When a Gatekeeper service is requested:
1. An authorization call to BackEnd is executed in order to verify user.
2. If user is authorized, service is executed.

MongoDB is used to store Users information.
<br>
Azure local storage is used for content hosting.
<br>
Valet Keys are used in order to retrieve content from Azure local storage.
<br>
When a BackEnd service fails, HTTP status 500 is returned to GateKeeper.
<br>
You can find requests execution examples in `requests_examples.txt`.
				 
