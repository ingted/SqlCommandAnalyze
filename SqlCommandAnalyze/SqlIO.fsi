namespace Tachyus.SqlCommandAnalyzer

/// Data Flow meta data
type SourcedData =
    {
    SelectorTypeName : string
    ScalarName : string
    DataBase : string option
    Schema : string option
    Table : string option
    Column : string option
    SystemTypeName : string option
    }

module SqlIO =

    /// ENDPOINT: retrieve data flow meta data from sys.dm_exec_describe_first_result_set
    val getTestFirstResultSet : conn : string -> SourcedData list

    /// ENDPOINT: retrieve execution plan as XML
    val testPlanSql : conn : string -> sql : string -> string

    val getRecentSourcedData : selectorTypeName : string -> conn : string -> SourcedData list

    val insertSourcedData : conn : string -> etlSourcedData : SourcedData list -> unit


