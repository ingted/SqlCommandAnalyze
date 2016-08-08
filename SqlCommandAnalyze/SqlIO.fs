namespace Tachyus.SqlCommandAnalyzer

open System
open System.Data
open System.Data.SqlClient
open FSharp.Data

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

    [<Literal>]
    let DesignTimeConn = "name=AnalyzeTest"

    type EtlSource = SqlProgrammabilityProvider<DesignTimeConn>

    type TestFirstResultSet = SqlCommandProvider<"
        SELECT * FROM sys.dm_exec_describe_first_result_set(N'
        select * FROM [Data].[All_Data_Types]
        ', NULL, 2)
        ", DesignTimeConn>

    let sortEtlSourcedData etlSourcedData =
        
        etlSourcedData.ScalarName, etlSourcedData.DataBase, etlSourcedData.Schema, etlSourcedData.Table, etlSourcedData.Column

    let getTestFirstResultSet (conn : string) =

        use cmd = new TestFirstResultSet(conn)

        let selectorTypeName = "TestFirstResultSet"

        cmd.Execute()
        |> Seq.filter (fun x -> 
            match x.name with
            | Some "ROWSTAT" -> false
            | Some x -> true
            | None -> false)
        |> Seq.fold (fun s t -> {SelectorTypeName = selectorTypeName; ScalarName = t.name.Value; DataBase = t.source_database; Schema = t.source_schema; Table = t.source_table; Column = t.source_column; SystemTypeName = t.system_type_name}::s) []
        |> List.distinct
        |> List.sortBy (fun x -> (sortEtlSourcedData x))

    let testPlanSql conn (sql : string) = 
        
        use sqlConn = new SqlConnection(conn)
        use sqlCommand1 = new SqlCommand("SET SHOWPLAN_XML ON", sqlConn)
        use sqlCommand2 = new SqlCommand(sql, sqlConn)

        sqlConn.Open()

        sqlCommand1.ExecuteNonQuery() |> ignore

        (sqlCommand2.ExecuteScalar()).ToString()

    type RecentSourcedData = SqlCommandProvider<"
        SELECT [SelectorTypeName]
              ,[ScalarName]
              ,[DataBase]
              ,[Schema]
              ,[Table]
              ,[Column]
              ,[SystemTypeName]
        FROM [Meta].[SourcedData]
        WHERE SelectorTypeName = @SelectorTypeName
        AND StateDateTime = (
            SELECT MAX(StateDateTime) AS StateDateTime FROM [Meta].[SourcedData]
            WHERE SelectorTypeName = @SelectorTypeName)
        ", DesignTimeConn>

    let getRecentSourcedData selectorTypeName (conn : string) =

        use cmd = new RecentSourcedData(conn)

        cmd.Execute(SelectorTypeName = selectorTypeName)
        |> Seq.fold (fun s t -> {SelectorTypeName = t.SelectorTypeName; ScalarName = t.ScalarName; DataBase = t.DataBase; Schema = t.Schema; Table = t.Table; Column = t.Column; SystemTypeName = t.SystemTypeName}::s) []
        |> List.sortBy (fun x -> (sortEtlSourcedData x))
        
    let insertSourcedData (conn : string) (etlSourcedData : SourcedData list)  =

        use conn = new SqlConnection(conn)
        use tran = conn.BeginTransaction(IsolationLevel.Serializable)

        use etlSourcedDataTable = new EtlSource.Meta.Tables.SourcedData()

        let timeNow = DateTime.UtcNow

        etlSourcedData
        |> List.iter (fun x -> 
            etlSourcedDataTable.AddRow(
                SelectorTypeName = x.SelectorTypeName
                ,ScalarName = x.ScalarName
                ,StateDateTime = timeNow
                ,DataBase = x.DataBase 
                ,Schema = x.Schema
                ,Table = x.Table
                ,Column = x.Column
                ,SystemTypeName = x.SystemTypeName) ) 

        etlSourcedDataTable.BulkCopy(conn, SqlBulkCopyOptions.Default, tran)

        tran.Commit()