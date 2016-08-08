# SQL Command Analyze #

A proposal for data flow meta data gathering within SQLCommandProvider

## SqlIO ##

File with proposed meta data endpoints and endpoints for meta data persistence. Sigature file exposes only proposed SQLCommandProvider endpoints.

- ENDPOINT: retrieve data flow meta data from sys.dm_exec_describe_first_result_set
>>val getTestFirstResultSet : conn : string -> SourcedData list

- ENDPOINT: retrieve execution plan as XML
>>val testPlanSql : conn : string -> sql : string -> string


## Analyze ##

Example of using meta data endpoints in a system to persist data flow information. 

Note that in most cases the first test result meta data is sufficient, but in cases (for instance) where a CASE statement is in the SQL, first test result cannot analyze the column involved. In this case that data can be retrieved by examining the execution plan XML.

## Entoleon_Proxy ##

Database for demo.

.bacpac included in repo. Connection string is in App.config.
