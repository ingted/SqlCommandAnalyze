namespace Tachyus.SqlCommandAnalyzer

open Nessos.FsPickler

open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Ast
open SqlIO
open System.IO
open System.Xml
open System.Xml.XPath

module Analyze =

    let checker = FSharpChecker.Create()

    let getUntypedTree (file, input) = 
        // Get compiler options for the 'project' implied by a single script file
        let projOptions = 
            checker.GetProjectOptionsFromScript(file, input)
            |> Async.RunSynchronously

        // Run the first phase (untyped parsing) of the compiler
        let parseFileResults = 
            checker.ParseFileInProject(file, input, projOptions) 
            |> Async.RunSynchronously

        match parseFileResults.ParseTree with
        | Some tree -> tree
        | None -> failwith "Something went wrong during parsing!"

    [<Literal>]
    let SqlCommandProvider = "SqlCommandProvider"
      
    let getLongIdentWithDots = function
        | LongIdentWithDots.LongIdentWithDots(longIdent, rangeList) ->
            (List.head longIdent).idText

    let getStaticConstant = function
        | SynType.StaticConstant(synConst, range) ->

            match synConst with
            | SynConst.String(theString, range) ->
                 theString.Trim() |> Some
            | _ ->
                None
        | _ -> 
            None

    let visitSynType = function
        | SynType.App(synType, rangeOption, synTypeList, rangeList, rangeOption2, boolean, range) ->
            let ident =
                match synType with
                | SynType.LongIdent(longIdentWithDots) ->
                    getLongIdentWithDots longIdentWithDots
                | _ -> ""

            if ident = SqlCommandProvider then
                getStaticConstant (List.head synTypeList)
            else
                None
        | _ -> 
            None
    
    let visitTypeDefnSimpleRepr = function
        | SynTypeDefnSimpleRepr.TypeAbbrev(parseDetail, synType, range) ->
            visitSynType synType
        | _ -> 
            None

    let visitTypeDefnRepr = function
        | SynTypeDefnRepr.Simple(synTypeDefnSimpleRepr, range) ->
            visitTypeDefnSimpleRepr synTypeDefnSimpleRepr
        | _ -> 
            None

    let visitTypeDefn sqls = function
        | SynTypeDefn.TypeDefn(synComponentInfo, synTypeDefnRepr, synMemberDefns, range) ->
            
            match visitTypeDefnRepr synTypeDefnRepr  with
            | Some x -> x::sqls
            | None -> sqls

    let rec visitDeclarations decls sqls = 

        Seq.fold (fun sqls (declaration : SynModuleDecl) ->
            match declaration with
            | SynModuleDecl.Types(synTypeDefns, longIdentWithDots) -> 
                
                synTypeDefns
                |> List.fold visitTypeDefn sqls

            | SynModuleDecl.NestedModule(synComponentInfo, synModuleDecls, isRec, range)  -> 
                visitDeclarations synModuleDecls sqls
            | _ -> 
                sqls) sqls decls

    let visitModulesAndNamespaces modulesOrNss =

        Seq.fold (fun sqls (moduleOrNs : SynModuleOrNamespace) ->
            let (SynModuleOrNamespace(lid, isMod, decls, xml, attrs, _, m)) = moduleOrNs
            visitDeclarations decls sqls) [] modulesOrNss

    let SqlCommandsFromFile file = 
    
        let astTree =
            (file, File.ReadAllText file)
            |> getUntypedTree 

        match astTree with
        | ParsedInput.ImplFile(implFile) ->
            // Extract declarations and walk over them
            let (ParsedImplFileInput(fn, script, name, _, _, modules, _)) = implFile
            visitModulesAndNamespaces modules
        | _ -> failwith "F# Interface file (*.fsi) not supported."


    let sourcedDataFromPlan conn selectorTypeName =
        let thePlan = //testPlanSql conn ""
            let path = __SOURCE_DIRECTORY__ + "\TestPlanXml.txt"
            File.ReadAllText(path)
        use strReader = new StringReader(thePlan)
        use  xreader = new XmlTextReader(strReader)
        let doc = new XPathDocument(xreader, XmlSpace.Preserve)
        let navigator = doc.CreateNavigator()
        let nsmgr = new XmlNamespaceManager(navigator.NameTable)
        nsmgr.AddNamespace("sql", "http://schemas.microsoft.com/sqlserver/2004/07/showplan")

        let queryScalarColumnReference = 
                navigator.Compile("/descendant::sql:ColumnReference")

        queryScalarColumnReference.SetContext(nsmgr)

        let scalarColumnReferenceIterator = navigator.Select(queryScalarColumnReference)

        let stripBrakets (x : string) = 
            x.Replace("[", "").Replace("]", "")
            |> Some

        let rec loop continue collection =
            if continue then
                match scalarColumnReferenceIterator.Current.GetAttribute("Database", "") with
                | "" -> (scalarColumnReferenceIterator.MoveNext(), collection) ||> loop
                | database ->
                    let column =
                        scalarColumnReferenceIterator.Current.GetAttribute("Column", "")
                        
                        
                    (scalarColumnReferenceIterator.MoveNext(),
                    {
                    SelectorTypeName = selectorTypeName
                    ScalarName = column
                    DataBase = database |> stripBrakets
                    Schema = scalarColumnReferenceIterator.Current.GetAttribute("Schema", "") |> stripBrakets
                    Table = scalarColumnReferenceIterator.Current.GetAttribute("Table", "") |> stripBrakets
                    Column = column |> stripBrakets
                    SystemTypeName = None}::collection)
                    ||> loop
            else 
                collection
                |> List.distinct

        (scalarColumnReferenceIterator.MoveNext(), [])
        ||> loop 

    let maintainSourcedData conn selectorTypeName =

        let testFirstResultSet = getTestFirstResultSet conn

        let recentEtlSourcedData = getRecentSourcedData selectorTypeName conn

        if testFirstResultSet = recentEtlSourcedData then ()
        else 
            if testFirstResultSet |> List.exists (fun x -> x.Column.IsNone) then
                testFirstResultSet
                |> List.filter (fun x -> x.Column.IsSome)
                |> List.append (sourcedDataFromPlan conn selectorTypeName)
                //SystemTypeName will be None if it came from sourcedDataFromPlan
                |> List.sortByDescending (fun x -> x.DataBase, x.Schema, x.Table, x.Column, x.SystemTypeName)
                |> List.distinctBy (fun x -> x.DataBase, x.Schema, x.Table, x.Column)
                |> insertSourcedData conn
            else
                insertSourcedData conn testFirstResultSet 


