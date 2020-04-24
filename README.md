# Amber Prototype based on Orleans

## Frontend installation(Linux): 
Before cloning the repo:
```
sudo apt-get install curl software-properties-common
curl -sL https://deb.nodesource.com/setup_12.x | sudo bash -
sudo apt-get install nodejs
```
Clone this repo then do the following:
```
cd TexeraOrleansBackend/TexeraOrleansPrototype/texera/core/new-gui
npm install
sudo npm audit fix
sudo npm install -g @angular/cli
sudo npm install --save-dev  --unsafe-perm node-sass
sudo npm audix fix
sudo ng build
```

## Amber Requirements:
1. Install dotnet-sdk 3.0
2. Install MySQL and create a user with username "orleansbackend" and password "orleans-0519-2019" (this can be changed at Constants.cs)
3. Create a mysql database called 'orleans'. Then, run the scripts [MySQL-Main.sql](https://github.com/dotnet/orleans/blob/master/src/AdoNet/Shared/MySQL-Main.sql), [MySQL-Clustering.sql](https://github.com/dotnet/orleans/blob/master/src/AdoNet/Orleans.Clustering.AdoNet/MySQL-Clustering.sql) to create the necessary tables and insert entries in the database.

## Run Amber:
### Start Silo:
Open terminal and enter:
```
cd TexeraOrleansBackend/TexeraOrleansPrototype/SiloHost
sudo dotnet run -c Release
```
### Start Client:
Open another terminal and enter:
```
cd TexeraOrleansBackend/TexeraOrleansPrototype/webapi-project
sudo dotnet run
```
### Create workflow through Web GUI
Go to `http://localhost:7070`, choose operators from left panel and link them together. Click the "Run" button in upper-right corner to  run the workflow. 


