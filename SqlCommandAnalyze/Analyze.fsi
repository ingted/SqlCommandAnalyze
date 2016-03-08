namespace Tachyus.SqlCommandAnalyzer

open Nessos.FsPickler

module Analyze =

    val SqlCommandsFromFile : file : string -> string list

    val maintainEtlSourcedData : conn : string -> selectorTypeName : string -> unit
