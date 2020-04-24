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
dotnet-sdk 3.0

## Run Amber:
### Start Client:
Open terminal and enter:
```
cd TexeraOrleansBackend/TexeraOrleansPrototype/webapi-project
sudo dotnet run
```
### Start Silo:
Open another terminal and enter:
```
cd TexeraOrleansBackend/TexeraOrleansPrototype/SiloHost
sudo dotnet run -c Release
```
### Create workflow through Web GUI
Go to `http://localhost:7070`, choose operators from left panel and link them together. Click the "Run" button in upper-right corner to  run the workflow. 


