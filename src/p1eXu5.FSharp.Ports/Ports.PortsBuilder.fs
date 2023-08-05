namespace p1eXu5.FSharp.Ports


type Port<'env, 'a> = Port of action: ('env -> 'a)


module Port =

    open System


    /// Run a Interpreter with a given environment
    let run env (Port action)  =
        action env  // simply call the inner function

    /// Create a Interpreter which returns the environment itself
    let ask = Port id

    /// Map a function over a Reader
    let map f port =
        Port (fun env -> f (run env port))

    /// flatMap a function over a Reader
    let bind f port =
        let newAction env =
            let x = run env port
            run env (f x)
        Port newAction

    /// The sequential composition operator.
    /// This is boilerplate in terms of "result" and "bind".
    let combine port1 port2 =
        port1 |> bind (fun () -> port2)

    /// The delay operator.
    let delay<'env, 'a> (func: unit -> Port<'env, 'a>) = func

    let retn v = (fun _ -> v) |> Port

    let withEnv f port =
        Port (fun env -> run (f env) port)

    let tryFinally compensation delayed =
        let action env =
            try
                run env delayed
            finally
                compensation ()
        Port action

    let using (f: 'a -> Port<_,_>) (v: #IDisposable) =
        tryFinally (fun () -> v.Dispose()) (f v)


module PortBuilderCE =

    type PortBuilder () =
        member _.Return(v) = Port.retn v
        member _.ReturnFrom(expr) = expr
        member _.Bind(m: Port<_,_>, f) = Port.bind f m
        member _.Zero() = Port (fun _ -> ())
        member _.Combine(expr1, expr2) = Port.combine expr1 expr2
        member _.Delay(func) = Port.delay func
        member this.While(guard, body) =
            if not (guard())
            then this.Zero()
            else this.Bind( body (), fun () ->
                this.While(guard, body))

        member _.TryFinally(body, compensation) = Port.tryFinally compensation body
        member _.Using(v, f) = Port.using f v
        member this.For(sequence: seq<_>, f) =
            this.Using(sequence.GetEnumerator(),fun enum ->
                this.While(enum.MoveNext,
                    this.Delay(fun () -> f enum.Current)))
        member _.Run(delay) =
            fun env ->
                let m = delay ()
                Port.run env m
            |> Port


    let port = PortBuilder()
