# Truffle Library
Truffle is a lightweight SQL management library written on top of ASP.net that aims to simplify interactions between a backend API and its SQL Databases. Programs written with this library are similar to Java Spring in the sense that they follow a Modeling framework with the use of Annotations to represent tables, columns and fields.

## Table of Contents
* [Introduction](#introduction)
* [Technologies](#technologies)
* [Structure Overview](#structure-overview)
* [Usage](#Usage)
* [Additional Notes](#additional-notes)

## Introduction
Truffle's classes provide 3 main utilities:
1. Running queries and procedures in an SQL database with automatic result parsing
2. Modeling and mapping of table columns to POCOs
3. Updating and inserting values into a table with direct mapping from created models

These functions may be utilised by instantiation and/or extension of Truffle's classes that allow for the flexible retrieval and modification of data.

## Technologies
* Dotnet version: 6.0

#### Dependencies
* System.Data.SqlClient Nuget Package provided by ASP.net

## Structure Overview
### Class Structure
The basic structure of Truffle is shown below:
```mermaid
classDiagram

class DatabaseConnector {
    - connection: SqlConnection
    + RunCommand(text: string): object
}

class DataCollector {
    + ReadValues(reader: SqlReader)$: object[]
    + ReadComplexValues(reader: SqlReader)$: List<Dictionary<string, object>>
}

class SqlObject {
    + GetAllValues(): Dictionary<string, object>
    + BuildAllRequest(): string
    + GetTable(): string
    + GetId(): string
}
<<abstract>> SqlObject

class PartialSqlObject {
    - values: Dictionary<string, object>
    + GetValue(key: string): object
    + GetString(key: string): string
    + GetDate(key: string): DateTime
    + GetBoolean(key: string): bool
}

class Column {
    + Name: string
}

class Table {
    + Name: string
}

class Id

class SqlEditor {
    - fields: Dictionary<string, string>
    + Set(column: string, value: object): void
    + SetAll(toAdd: Dictionary<string, object>): void
    - GetFields(): Dictionary<string, string>
}

class SqlInserter {
    Insert(table: string, database: DatabaseConnector): bool
}

class SqlUpdater {
    Update(table: string, database: DatabaseConnector): bool
}

DatabaseConnector *-- SqlConnection
DatabaseConnector --> DataCollector
SqlObject --> DatabaseConnector
SqlObject --> Id
SqlObject --> Column
SqlObject --> Table
SqlObject <|-- PartialSqlObject
SqlEditor <|-- SqlUpdater
SqlEditor <|-- SqlInserter
SqlUpdater --> DatabaseConnector
SqlInserter --> DatabaseConnector
```
* `DatabaseConnector` is a wrapper class that holds an `SqlConnection`. It provides a method to run SQL queries and collect result values with the use of `DataCollector`
* `DataCollector` is a static class that provides methods to collect values from an SQL query into  `object[]` and `Dictionary` formats
* `SqlObject` is an abstract class that represents a model of an entry in a sql table, and classes that extend it may be instantiated with a `DatabaseConnector`. `SqlObject` maps values to properties with the use of annotations `Column`, `Table` and `Id` as flags
* `PartialSqlObject` is an extension of `SqlObject` that retrieves column values even if their corresponding properties are not present in the model. This is useful for large tables with many columns that may need to be returned but not read
* `SqlEditor` is an abstract class that provides methods to collect parameters into a `Dictionary` to perform subsequent SQL queries
* `SqlInserter` and `SqlUpdater` are both classes used for table modification that extend the `SqlEditor` class and provide methods to insert and update data in an SQL table.

## Usage
### Sql Databases and Queries
The connection to an SQL database is created when a `DatabaseConnector` object is instantiated with a connection string:

```C#
public void ConnectToDatabaseExample()
{
    // Get a connection string
    string str = "YourConnectionString";
    using (DatabaseConnector database = new DatabaseConnector(str))
    {
        // Use the database here to run sql queries or to create SqlObjects
    }
}
```
`DatabaseConnector` is disposable and should hence be used in a disposable context (either with the `using` keyword or by calling `Dispose()` when it is no longer needed).

Once it is instantiated, it may then be used to run regular SQL queries or procedures, or create instances of SqlObjects.

### Running Commands
Sql queries may be run with the `DatabaseConnector` by calling its `RunCommand()` method:
```C#
using (DatabaseConnector database = new DatabaseConnector(str))
{
    var response = (object[]) database.RunCommand("select * from [dbo].[yourtable]");
}
```
By default, `RunCommand()` returns an `object[]` of all the values from the query, but it may be configured by adding an additional `complex` argument and setting it to `true` for it to return a more complex output:
```C#
var response = (List<Dictionary<string, object>>) database.RunCommand("mycommand", complex: true);
```
This is useful when handling a query that would return multiple rows of data, or if you wish to have your values mapped to their respective column names.

### Running Procedures
Procedure calls are similarly made with the `RunCommand()` method, but with the configuration of additional parameters to execute the call:
```C#
using (DatabaseConnector database = new DatabaseConnector(str))
{
    var procedureName = "MyProcedure";
    string[] parameters = ["param1", "param2"]; // Your procedure parameters
    var response = (object[]) database.RunCommand(procedureName, true, parameters);
}
```
`RunCommand` takes `true` as a second argument to indicate that the command is a procedure instead of a regular query. It then uses the `string[]` provided as a third argument as parameters to execute the procedure.

Similar to a regular query, `complex` may also be set to `true` for a more substantial output.

### Creating a Model
In order to make use of the modelling capabilities of Truffle, we must first create a POCO (Plain old C# Object) that represents an entry in our table with some additional annotations:
```C#
[Table("[dbo].[tblDog]")]
public class Dog : SqlObject
{
    [Id, Column("Name")]
    public string MyName {get;set;}
    [Column("Age")]
    public int Age {get;set;}
    [Column("Owner")]
    public string Owner {get;set;}
    [Column("dob")]
    public DateTime DateOfBirth {get;set;}
}
```

There are some important things to note:
1. `Dog` extends the SqlObject class which provides it with its required additional methods and mappings.
2. `Table` is used to indicate the table which stores the data of `Dog`.
3. `Column` is used to indicate the column which the property corresponds to.
4. `Id` is used to indicate that the column is a `Key` (unique). There should only be one `Id` for every object.

These annotations are based on Java Spring's Database API, so developers who have used the framework will find these familiar.

We can then make use of several of `SqlObject`'s built-in constructors to instantiate our model in different ways:

#### Instantiating by Key
`SqlObject` can be instantiated by accepting a value for its `Id` column:
```C#
[Table("[dbo].[tblDog]")]
public class Dog : SqlObject
{
    [Id, Column("Name")]
    public string Name {get;set;}
    // ...
    public Dog(object value, DatabaseConnector db): base(value, db) {}
}

public static class Main
{
    public static void Run()
    {
        using (DatabaseConnector database = new DatabaseConnector("MyConnectionString"))
        {
            // Get a dog with Name='Spot'
            Dog dog = new Dog("Spot", database); 

            // Prints "Hi, my name is Spot!"
            Console.WriteLine($"Hi, my name is {dog.Name}!");
        }
    }
}
```
Note that the base constructor needs to be called in the inherited class for this to work. This is true for **all** constructors provided by `SqlObject`

#### Instantiating by Column
An `SqlObject` can also be instantiated by identifying an entry by column. In the case that there are multiple results for the identifier, only the first one is mapped and returned.

```C#
[Table("[dbo].[tblDog]")]
public class Dog : SqlObject
{
    [Column("Owner")]
    public string Owner {get;set;}
    // ...
    public Dog(object value, string key, DatabaseConnector db): base(value, key, db) {}
}

public static class Main
{
    public static void Run()
    {
        using (DatabaseConnector database = new DatabaseConnector("MyConnectionString"))
        {
            // Get a dog with Owner = Tom'
            Dog dog = new Dog("Tom", "Owner", database); 

            // Prints "Hi, my name is Owner!"
            Console.WriteLine($"Hi, my owner is {dog.Owner}!");
        }
    }
}
```

#### Instantiating with Values
In the case that a model should be instantiated with values instead of directly from a database, this may be done by passing a `Dictionary` into its constructor:
```C#
public static void Run()
    {
        using (DatabaseConnector database = new DatabaseConnector("MyConnectionString"))
        {
            //Gets all entries from a table
            string command = $"select * from {new Dog().GetTable()}";
            var response = (List<Dictionary<string, object>>) database.RunCommand(command, complex:true);
            
            Console.WriteLine("This is a list of dogs.");
            foreach (var item in response)
            {
                // Instantiate Dog with a dictionary
                Dog dog = new Dog(item);
                
                Console.WriteLine(dog.Name);
            }
        }
    }
```

#### Instantiating without Values
An `SqlObject` may also be instantiated without any arguments:
```C#
Dog dog = new Dog();
```
This may be useful when creating new entries for a table that do not have values at the time of creation.

### Reading a Model
An `SqlObject` has several built-in methods that are useful in interacting with it.

* `LogValues()` Is a method for debugging that logs all properties in an SqlObject
* `GetAllValues()` Is a method that returns a `Dictionary` of all the properties of the model corresponding to a column.
* `GetId()` and `GetTable()` returns the value of the `Table` and `Id` annotations of the model, which may be used for encapsulation of the model.
* `BuildAllRequest()` returns an Sql query which selects all rows in a table and returns all values with a corresponding property in the model.

### Using PartialSqlModel
There are some cases in which column values need to be stored, but they are not used actively in a model (Eg. an API service which might need to select all columns for an entry, but does not reference them internally).

In such cases, `PartialSqlObject` may be used instead of `SqlObject` to reduce the amount of boilerplate code that is required.

`PartialSqlObject` is an extension of `SqlObject` that selects ALL(*) columns in a table when creating an object, and stores them regardless of whether they correspond to a property in the model. This means that `GetAllValues()` returns the full result of the table without needing to define unused properties.

```C#
[Table("[dbo].[tblDog]")]
public class Dog : PartialSqlObject
{
    [Id, Column("Name")]
    public string Name {get;set;}
    public Dog(object value, DatabaseConnector db): base(value, db) {}
}

public static class Main
{
    public static void Run()
    {
        using (DatabaseConnector database = new DatabaseConnector("MyConnectionString"))
        {
            // Get a dog with Name = Spot'
            Dog dog = new Dog("Spot", database); 

            // Modify its properties
            dog.Name = "Spotto";

            // Get all its values and print them
            // The output will have ALL the columns from [tblDog]
            // Its new Name property will also be reflected
            Dictionary<string, object> values = dog.GetAllValues();
            foreach (string key in values.Keys)
            {
                Console.WriteLine($"{key}: {values[key]}");
            }
        }
    }
}
```

### Updating a table
Truffle provides an `SqlUpdater` class which provides flexible formats to update a table:

```C#
public static void Run()
{
    using (DatabaseConnector database = new DatabaseConnector("MyConnectionString"))
    {
        // Creates an SQL updater and sets its target to rows where Name='Spot'
        SqlUpdater updater = new SqlUpdater("Spot", "Name");
        
        // Adds values for the updater to set
        updater.Set("Age", 3);
        updater.Set("Owner", "Mike");

        // Updates the corresponding table in the database with new values
        updater.Update("[dbo].[tblDog]", database);
    }
}
```

`SqlUpdater` also directly accepts an `SqlObject` and will update all of its values accordingly:

```C#
public static void Run()
{
    using (DatabaseConnector database = new DatabaseConnector("MyConnectionString"))
    {
        // Instantiates a Dog from the database
        Dog dog = new Dog("Spot", database);

        // Change values
        dog.Owner = "Mike";

        // Creates an SQL updater and sets its target to rows where Name='Spot'
        SqlUpdater updater = new SqlUpdater(dog);

        // Updates the corresponding table in the database with new values
        updater.Update(dog.GetTable(), database);
    }
}
```

### Inserting to a Table
Truffle provides an `SqlInserter` class which provides flexible formats to update a table, in similar fashion to `SqlUpdater`:
```C#
public static void Run()
{
    using (DatabaseConnector database = new DatabaseConnector("MyConnectionString"))
    {
        // Creates an SQL updater and sets its target to rows where Name='Spot'
        SqlInserter inserter = new SqlInserter("Spot", "Name");
        
        // Adds values for the updater to set
        inserter.Set("Age", 3);
        inserter.Set("Owner", "Mike");

        // Updates the corresponding table in the database with new values
        inserter.Update("[dbo].[tblDog]", database);
    }
}
```

`SqlInserter` may also be instantiated with an `SqlObject` with similar behaviour to `SqlUpdater`.

## Additional Notes
* Truffle is still under heavy development and all of its classes are still subject to change.
* Planned additions include an `SqlSelector` or `SqlFilterer` class that would automate the filtering of tables by multiple parameters, as well as an `SqlObjectBuilder` that would allow for even more flexible creation of SqlObjects.