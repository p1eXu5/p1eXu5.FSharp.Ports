namespace p1eXu5.FSharp.Ports.Tests

open NUnit.Framework
open FsUnit
open FsToolkit.ErrorHandling
open p1eXu5.FSharp.Testing.ShouldExtensions

open p1eXu5.FSharp.Ports
open p1eXu5.FSharp.Ports.PortBuilderCE
open p1eXu5.FSharp.Ports.PortResultBuilderCE

module PortResultTests =

    [<Test>]
    let ``Bind with port`` () =
        let aPort = port { return 1 }
        let add1PortResult =
            portResult {
                let! a = aPort
                return a + 1
            }

        let res = add1PortResult |> PortResult.run ()
        res |> Result.shouldEqual 2

    [<Test>]
    let ``Bind with result`` () =
        let aOkResult = result { return 1 }
        let add1PortResult =
            portResult {
                let! a = aOkResult
                return a + 1
            }

        let res = add1PortResult |> PortResult.run ()
        res |> Result.shouldEqual 2

    [<Test>]
    let ``Bind with failure result`` () =
        let aFailedResult = Error "test error"
        let add1PortResult =
            portResult {
                let! a = aFailedResult
                return a + 1
            }

        let res = add1PortResult |> PortResult.run ()
        res |> should be (ofCase <@ Result<int, string>.Error @>)
