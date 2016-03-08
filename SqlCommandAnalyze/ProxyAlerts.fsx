#I @"..\packages\FSharp.Data\lib\net40"
#r "FSharp.Data.dll"
open FSharp.Data

type ElasticResponse = JsonProvider<"""{"took":138,"timed_out":false,"_shards":{"total":605,"successful":603,"failed":0},"hits":{"total":574,"max_score":16.803278,"hits":[{"_index":"logstash-2016.02.02","_type":"logs","_id":"AVKgyAOJYxHf95u0P4XM","_score":16.803278,"_source":{"@timestamp":"2016-02-02T07:00:01.328Z","counter":"4","logger":"default","level":"INFO","message":"process completed","company":"Seneca","server":"tcp:172.19.1.11,54998","database":"Seneca_Proxy","process":"ImportConsole","machinename":"TACHYUSCUSTDB","@version":"0.0.0.0","host":"172.19.1.11","tags":["tcpjson"]}}]}}""">

let requestElk bodyJson =

    Http.RequestString ("http://elk001.tachyus.com:9200/_search", 
        headers = [ HttpRequestHeaders.BasicAuth "viewer" "W7h2fjqY3qMypVXu" ],
        body = TextRequest bodyJson)

let elkResponseLength elasticResponse =
    ElasticResponse.Parse(elasticResponse).Hits.Hits.Length

let isProcessAlive company proxyProcess hoursBack =

    (proxyProcess, company, hoursBack)
    |||> sprintf """ 
    {
      "query": {
            "and": [
                {"match" : {"process": "%s"}},
                {"match" : {"company": "%s"}},
                {"match" : {"message" : "process completed"}},
                {"range" : {"@timestamp" : {"gte" : "now-%ih"}}}
              ]
       }
    }""" 
    |> requestElk
    |> elkResponseLength
    |> (<) 0 
    |> printfn "%s %s is alive : %b" company proxyProcess

let isCompanyAlive company proxyProcess hoursBack =

    (proxyProcess, company, hoursBack)
    |||> sprintf """ 
    {
      "query": {
            "and": [
                {"match" : {"process": "%s"}},
                {"match" : {"company": "%s"}},
                {"match" : {"message" : "end company"}},
                {"range" : {"@timestamp" : {"gte" : "now-%ih"}}}
              ]
       }
    }""" 
    |> requestElk
    |> elkResponseLength
    |> (<) 0 
    |> printfn "%s %s is alive : %b" company proxyProcess

let noErrors proxyProcess hoursBack =
    
    (proxyProcess, hoursBack)
    ||> sprintf """ 
    {
      "query": {
            "and": [
                {"match" : {"process": "%s"}},
                {"match" : {"level" : "ERROR"}},
                {"range" : {"@timestamp" : {"gte" : "now-%ih"}}}
              ]
       }
    }""" 
    |> requestElk
    |> elkResponseLength
    |> (=) 0 
    |> printfn "%s no errors : %b" proxyProcess


[<Literal>]
let Freeport = "Freeport"

[<Literal>]
let Seneca = "Seneca"

[<Literal>]
let Vaquero = "Vaquero"

[<Literal>]
let ImportConsole = "ImportConsole"

[<Literal>]
let DropZoneToFileTableConsole = "DropZoneToFileTableConsole"

[<Literal>]
let IlionConsole = "IlionConsole"

[<Literal>]
let EntoleonLogService = "EntoleonLogService"

[<Literal>]
let PersistProxyMonitor = "PersistProxyMonitor"

isCompanyAlive Freeport DropZoneToFileTableConsole 1
isCompanyAlive Freeport PersistProxyMonitor 25
isCompanyAlive Freeport IlionConsole 25
isCompanyAlive Freeport EntoleonLogService 1
printfn ""
isProcessAlive Seneca ImportConsole 4
noErrors ImportConsole 25
isCompanyAlive Seneca DropZoneToFileTableConsole 1
isCompanyAlive Seneca PersistProxyMonitor 25
printfn ""
isCompanyAlive Vaquero DropZoneToFileTableConsole 1
isCompanyAlive Vaquero PersistProxyMonitor 25
isCompanyAlive Vaquero IlionConsole 25
isCompanyAlive Vaquero EntoleonLogService 1
printfn ""
noErrors DropZoneToFileTableConsole 24
noErrors IlionConsole 24
noErrors EntoleonLogService 24
noErrors PersistProxyMonitor 24

