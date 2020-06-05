# Amber Prototype based on Orleans

## Introduction
Long-running analytic tasks on big data frameworks often provide little or no feedback about the status of the execution. Some big  data  processing frameworks provide status updates for running jobs, but these  systems  only  allow  users  to  monitor  their  jobs passively.  Even if the users notice anomalies happening during  the  execution,  they  can  either  kill  the  job  or wait for the job to run to its completion.

Amber is a distributed data processing engine build on top of existing actor model implementation. It has a unique capability of supporting responsive debugging during the execution of a dataflow. Users can pause/resume the execution, investigate the state of operators, change the behavior of an operator, and set conditional breakpoints. Amber provides these features along with the support for fault tolerance. In case of a failure, it not only ensures the correctness of the final computation result, but also recovers the same consistent debugging state.

**Paper**: [Amber: A Debuggable Dataflow System Based on the
Actor Model](http://www.vldb.org/pvldb/vol13/p740-kumar.pdf)(VLDB 2020)

**Contributors**: Shengquan Ni, Avinash Kumar, Zuozhi Wang, Chen Li.

**Affiliation**: University of California, Irvine.

## Install Frontend

### Install Node JS

- For Windows / Mac

  Download and install the latest LTS version of [NodeJS](https://nodejs.org/en/) (Version 12)

- For Linux
  ```
  sudo apt-get install curl software-properties-common
  curl -sL https://deb.nodesource.com/setup_12.x | sudo bash -
  sudo apt-get install nodejs
  ```

### Build Frontend
Clone this repo then do the following:
```
cd AmberOnOrleans/Frontend
npm install
npm run build
```
Running `npm install` will take a long time, usually 5 to 10 minutes. You can ignore the vulnerabilities warnings in the end.

## Install Amber

1. Install [dotnet-sdk 3.0](https://dotnet.microsoft.com/download)
2. Install MySQL and login as admin. Using the following command to create a user with username "orleansbackend" and password "orleans-0519-2019" (this can be changed at [Constants.cs](https://github.com/Hiseen/OrleansExp/blob/master/TexeraOrleansPrototype/Utilities/Constants.cs))
```
CREATE USER 'orleansbackend'@'%' IDENTIFIED BY 'orleans-0519-2019';
```
3. Create a mysql database called 'amberorleans' and grant all privileges by using the following commands.
```
CREATE DATABASE amberorleans;
GRANT ALL PRIVILEGES ON amberorleans. * TO 'orleansbackend'@'%';
FLUSH PRIVILEGES;
USE amberorleans;
```
4. Run the scripts [MySQL-Main.sql](https://github.com/dotnet/orleans/blob/master/src/AdoNet/Shared/MySQL-Main.sql), [MySQL-Clustering.sql](https://github.com/dotnet/orleans/blob/master/src/AdoNet/Orleans.Clustering.AdoNet/MySQL-Clustering.sql) to create the necessary tables and insert entries in the database. 

5. We have generated some sample dataset for you to banchmark Amber, here are 2 datasets you can use:
   - [tiny TPC-H dataset(MBs)](https://drive.google.com/file/d/1S0TFQ80D6xqZcUECqBAWNGc9XW6AttCs/view?usp=sharing)
   - [TPC-H sample dataset(1GB)](https://drive.google.com/file/d/1h4zVUABmMp9dA2YXb2faH4O9ULUDcimY/view?usp=sharing)
   
   Download one dataset from the links above to your local machine.

## Run Amber on your local machine:
### 1.Start MySql Server on local machine.
### 2.Start Silo:
Slio is a container of actors in Orleans where all the computation takes place. We need to start Silo first so that Amber knows where to allocate actors.

Open terminal and enter:
```
cd AmberOnOrleans/SiloHost
dotnet run -c Release
```
You can ignore all the warnings and it takes time to build the connection.

Make sure you see "Silo Started!" before proceeding to step 3.
### 3.Start Console Application:
Open another terminal and enter:
```
cd AmberOnOrleans/ConsoleApp
dotnet run
```
It will prompt you to choose a sample workflow and enter the path of the dataset on your local machine.

After entering all the parameters, the workflow will automatically run and the results will be displayed.
### 4.Create workflow through Web GUI(Optional):
If you want to checkout the web-based frontend of Amber. This is a step-by-step guide for creating and runnning a sample Workflow using one of the datasets above.

Open another terminal and enter:
```
cd AmberOnOrleans/WebApp
dotnet run
```

Go to `http://localhost:7070`, you can see a web GUI for Amber:
![web GUI](http://drive.google.com/uc?export=view&id=15_-lT_asJ6YzePln4tVvvNrGRnoqK7Th)

Drag Source -> Scan operator from left panel and drop it on the canvas:
![Scan](http://drive.google.com/uc?export=view&id=1OJ-MsaK5ISMuyzuWuXjX_W5KpzyVc_yX)

Then, drag and drop Utilities -> Comparison, LocalGroupBy, GlobalGroupBy and Sort -> Sort respectively. They will automatically be linked with the previous operator. Your workflow should look like this:
![W1](http://drive.google.com/uc?export=view&id=1mvv7J6QVYEXHEQKYQRmuGwrn1pBy6mMo)

You can specifiy properties for each operator on the right panel. Each operator should have the following properties:

Scan:

![Scan properties](http://drive.google.com/uc?export=view&id=1qf2q8eEglarhQ1mr3ajF5L6DNHQLtcrX)

Comparison:

![Comparison properties](http://drive.google.com/uc?export=view&id=1lBGkUF4tIyry5zqkQVgvogRgZoHCQgZP)

LocalGroupBy:

![LocalGroupBy properties](http://drive.google.com/uc?export=view&id=1C_EYg2g6S9FT_xFI_Su5CGYWfuXVFgYi)

GlobalGroupBy:

![GlobalGroupBy properties](http://drive.google.com/uc?export=view&id=1YFiRbyXZzszDGM2e8sY3JN8Uzmiha1Ms)

Sort:

![Sort properties](http://drive.google.com/uc?export=view&id=1QzqOalYv4oMBMnx23orl6gMlMtn1MY-v)

Click the "Run" button in upper-right corner to run the workflow. After completion, the following result will pop up from the bottom:

![result](http://drive.google.com/uc?export=view&id=1HG7cnoXKgXdpjYFX4r2DZkuga8JaFP19)


## Run Amber on a cluster:
### 1.Clone this repo:
On one cluster machine (name it A) which installed MySql Server and do the following change at [Constants.cs](https://github.com/Hiseen/OrleansExp/blob/master/TexeraOrleansPrototype/Utilities/Constants.cs):
```
public static string ClientIPAddress = <Current Machine's IP address>;
...
public volatile static int DefaultNumGrainsInOneLayer = <# of Machines in the cluster - 1>;
```
### 2.Start MySql Server on machine A.
### 3.Copy the edited repo to all other machines in the cluster.
### 4.Start Silos:
Slio is a container of actors in Orleans where all the computation takes place. We need to start Silo first so that Amber knows where to allocate actors.

Open terminal and enter on all other machines in the cluster:
```
cd AmberOnOrleans/SiloHost
dotnet run -c Release
```
You can ignore all the warnings and it takes time to build the connection.

Make sure you see "Silo Started!" on all the machines before proceeding to step 4.
### 5.On machine A, follow from step 3 or 4 of the tutorial above.

