type EtlSourcedData =
    {
    SelectorTypeName : string
    ScalarName : string
    DataBase : string option
    Schema : string option
    Table : string option
    Column : string option
    SystemTypeName : string option
    }

let t = 
    [
    {
    SelectorTypeName = "X"
    ScalarName = "1"
    DataBase = Some "B"
    Schema = Some "D"
    Table = Some "T"
    Column = Some"C2"
    SystemTypeName = Some "T"
    };
    {
    SelectorTypeName = "X"
    ScalarName = "1"
    DataBase = Some "B"
    Schema = Some "D"
    Table = Some "T"
    Column = Some"C1"
    SystemTypeName = Some "T"
    };
    {
    SelectorTypeName = "X"
    ScalarName = "1"
    DataBase = Some "B"
    Schema = Some "D"
    Table = Some "T"
    Column = Some"C3"
    SystemTypeName = Some "T"
    };
    {
    SelectorTypeName = "X"
    ScalarName = "1"
    DataBase = Some "B"
    Schema = Some "D"
    Table = Some "T"
    Column = Some"C2"
    SystemTypeName = None
    };
    {
    SelectorTypeName = "X"
    ScalarName = "1"
    DataBase = Some "B"
    Schema = Some "D"
    Table = Some "T"
    Column = Some"C4"
    SystemTypeName = Some "T"
    }
    ]

let y =
    t
    |> List.sortByDescending (fun x -> x.DataBase, x.Schema, x.Table, x.Column, x.SystemTypeName)
    |> List.distinctBy (fun x -> x.DataBase, x.Schema, x.Table, x.Column)


#I @"..\packages\FSharp.Data\lib\net40"
#r "FSharp.Data.dll"
open FSharp.Data
open FSharp.Reflection

type RecordType = { field1 : string; field2 : RecordType option; field3 : (unit -> RecordType * string) }

let rec recordtype1 : RecordType = { field1 = "field1"; field2 = Some(recordtype1); field3 = ( fun () -> (recordtype1,"")  )}
let makeRecord = FSharpValue.MakeRecord(typeof<RecordType>,[|box"field1";box(Some(recordtype1));box( fun () -> (recordtype1,"")) |])
let makeRecord2 = FSharpValue.MakeRecord(typeof<RecordType>,[|box"field1";box(Some(recordtype1));box( fun () -> (recordtype1,"")) |], System.Reflection.BindingFlags.Default)

type ElasticResponse = JsonProvider<"""{"took":138,"timed_out":false,"_shards":{"total":605,"successful":603,"failed":0},"hits":{"total":574,"max_score":16.803278,"hits":[{"_index":"logstash-2016.02.02","_type":"logs","_id":"AVKgyAOJYxHf95u0P4XM","_score":16.803278,"_source":{"@timestamp":"2016-02-02T07:00:01.328Z","counter":"4","logger":"default","level":"INFO","message":"process completed","company":"Seneca","server":"tcp:172.19.1.11,54998","database":"Seneca_Proxy","process":"ImportConsole","machinename":"TACHYUSCUSTDB","@version":"0.0.0.0","host":"172.19.1.11","tags":["tcpjson"]}}]}}""">

let x = 
    Http.RequestString ("http://elk001.tachyus.com:9200/_search?q=message:etl process completed", 
        headers = [ HttpRequestHeaders.BasicAuth "viewer" "W7h2fjqY3qMypVXu" ])


let z = 
    Http.RequestString ("http://elk001.tachyus.com:9200/_search?q=process:ImportConsole AND company:Seneca AND message:process completed&range=timestamp:gte now-24", 
        headers = [ HttpRequestHeaders.BasicAuth "viewer" "W7h2fjqY3qMypVXu" ])

let senecaImportConsoleIsAlive = 
    Http.RequestString ("http://elk001.tachyus.com:9200/_search", 
        headers = [ HttpRequestHeaders.BasicAuth "viewer" "W7h2fjqY3qMypVXu" ],
        body = TextRequest """ 
        {
  "query": {
        "and": [
            {"match" : {"process": "ImportConsole"}},
            {"match" : {"company": "Seneca"}},
            {"match" : {"message" : "process completed"}},
            {"range" : {"@timestamp" : {"gte" : "now-4h"}}}
          ]
   }
}
         """ )

let senecaImportConsoleIsAliveResponse = ElasticResponse.Parse(senecaImportConsoleIsAlive).Hits.Hits.Length

//let l = senecaImportConsoleIsAliveResponse.Length
//let company = senecaImportConsoleIsAliveResponse.[0].Source.Company

let senecaImportConsoleNoErrors = 
    Http.RequestString ("http://elk001.tachyus.com:9200/_search", 
        headers = [ HttpRequestHeaders.BasicAuth "viewer" "W7h2fjqY3qMypVXu" ],
        body = TextRequest """ 
        {
  "query": {
        "and": [
            {"match" : {"process": "ImportConsole"}},
            {"match" : {"company": "Seneca"}},
            {"not" : {match : {"level" : "INFO"}}},
            {"range" : {"@timestamp" : {"gte" : "now-25h"}}}
          ]
   }
}
         """ )

let senecaImportConsoleNoErrorsResponse = ElasticResponse.Parse(senecaImportConsoleNoErrors).Hits.Hits.Length

//let l2 = senecaImportConsoleNoErrorsResponse.Length


printfn "%s" z
