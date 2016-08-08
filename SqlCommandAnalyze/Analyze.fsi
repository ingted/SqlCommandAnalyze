namespace Tachyus.SqlCommandAnalyzer

open Nessos.FsPickler

module Analyze =

    val SqlCommandsFromFile : file : string -> string list

    /// 
    val maintainSourcedData : conn : string -> selectorTypeName : string -> unit
