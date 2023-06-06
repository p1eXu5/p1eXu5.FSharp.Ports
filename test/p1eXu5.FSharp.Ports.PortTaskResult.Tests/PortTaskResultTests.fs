namespace p1eXu5.FSharp.Ports.Tests

open System
open System.Threading.Tasks

open NUnit.Framework
open FsUnit
open FsToolkit.ErrorHandling

open p1eXu5.FSharp.Ports
open p1eXu5.FSharp.Ports.PortTaskResult
open p1eXu5.FSharp.Ports.PortTaskResultBuilderCE
open p1eXu5.FSharp.Ports.Tests.Tasks

module PortTaskResultTests =

    [<Test>]
    let ``Bind with succeeded taskResult test``() =
        let success1 () = taskResult { return! Ok 1 }

        let sut = portTaskResult {
                let! a = success1 ()
                return a + 2
            }

        let res = sut |> runSynchronously ()
        res |> should equal 3

    [<Test>]
    let ``Bind with succeeded taskResult and task returning result test``() =
        let success1 () = taskResult { return! Ok 1 }
        let success2 () = task { return Ok 2 }

        let sut =
            portTaskResult {
                let! a = success1 ()
                let! b = success2 ()
                return a + b
            }

        let res = sut |> runSynchronously ()
        res |> should equal 3

    [<Test>]
    let ``Bind with succeeded taskResult and task returning value test``() =
        let success1 () = taskResult { return! Ok 1 }
        let success2 () = task { return 2 }

        let sut =
            portTaskResult {
                let! a = success1 ()
                let! b = success2 () |> PortTaskResult.fromTaskT
                return a + b
            }

        let res = sut |> runSynchronously ()
        res |> should equal 3

    [<Test>]
    let ``Bind with succeeded taskResult and value task returning value test``() =
        let success1 () = taskResult { return! Ok 1 }

        let sut =
            portTaskResult {
                let! a = success1 ()
                let! b = TestValueTaskFactory.SimpleValueTaskWithReturn(3) |> PortTaskResult.fromTaskT
                return a + b
            }

        let res = sut |> runSynchronously ()
        res |> should equal 3