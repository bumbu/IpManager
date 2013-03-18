# IpManager

Simple application that stores and has ability to change IPv4 network settings.
![Application screen](https://github.com/bumbu/IpManager/blob/master/images/screen.png?raw=true)

## Prerequisites

* VisualStudio 2012
* .NET 4.5
* [Parse](https://parse.com) account with cloud application that has all the fields from 
`class HostData` attributes list
> For VS2010 and .NET 4.0 look in branch old

## How to run

* Clone the repository and open the Visual Studio solution
* Rename `settings.json.sample` to `settings.json` and place in the same folder with application
* In Program.cs replace `YOUR APPLICATION ID` and `YOUR WINDOWS KEY` with your keys from [Parse](https://parse.com)
* Build and run _(run as administator in order to be able to change network settings)_

## ToDo

* Select device/interface for which IP settings are changing
* Store data into cloud
* Check validity of data
* Ability to reorder elements using Drag&Drop
* Add suppost for IPv6