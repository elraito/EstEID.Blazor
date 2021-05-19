# EstEID.Blazor

This is an example how to utilize PC/SC library in dotnet in order to read some basic info from Estonian Id card. 

A background worker service SmartCardService is created that listens to all Smart Card readers attached to the PC.
When a card is inserted the worker attempots to read the public personal data file.
Once read it fires and evnt through EventService that then update subscribers can use to display data.
Blazor is used to display the data from event.

This project is a showcase scaffold for services where authentication is not required.

When additional business logic is added the use cases could be:
1) Public events where security guard outfitted with a tablet + card reader needs for example quickly check entrees legal age.
2) Registering attendees in conferences
3) Keeping account of employees present in a company.
4) ...

There are no error checks whatsoever as its a simple demo.
Feel free to copy paste anything from here without credit attribution.
