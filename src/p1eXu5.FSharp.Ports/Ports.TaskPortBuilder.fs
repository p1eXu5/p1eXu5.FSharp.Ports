namespace p1eXu5.FSharp.Ports

open System.Threading.Tasks


type TaskPort<'config, 'a> = TaskPort of action: ('config -> Task<'a>)


module TaskPort =

    open System


    /// Run a Interpreter with a given environment
    let run env (TaskPort action)  =
        action env  // simply call the inner function
        //|> Async.AwaitTask
        //|> Async.RunSynchronously

    let runSynchronously env (TaskPort action)  =
        action env
        |> Async.AwaitTask
        |> Async.RunSynchronously

    /// Create a Interpreter which returns the environment itself
    let ask = Port (fun env -> env) 

    /// Map a function over a Reader
    let map f reader =
        TaskPort (fun env -> f (run env reader))

    /// flatMap a function over a Reader
    let bind f m =
        let newAction env =
            task {
                let! x = run env m
                return! run env (f x)
            }
        TaskPort newAction

    let retn v = (fun _ -> task { return v }) |> TaskPort

    let withEnv f interpreter =
        TaskPort (fun env -> run (f env) interpreter)

    let tryFinally compensation delayed =
        let action env =
            task {
                try
                    return! run env delayed
                finally
                    compensation ()
            }
        TaskPort action


    let using (f: 'a -> TaskPort<_,_>) (v: #IDisposable) =
        tryFinally (fun () -> v.Dispose()) (f v)

    let tee (f: 'a -> unit) (m: TaskPort<_,_>) =
        fun env ->
            task {
                let! x = run env m
                do
                    f x
                return x
            }
        |> TaskPort


module TaskPortBuilderCE =

    type TaskPortBuilder () =
        member _.Return(v) = TaskPort.retn v
        member _.ReturnFrom(expr: TaskPort<_,_>) = expr
        member _.ReturnFrom(expr: Port<_,_>) =
            fun env ->
                task {
                    return Port.run env expr
                }
            |> TaskPort

        member _.ReturnFrom(expr: Task<_>) =
            fun env ->
                task {
                    return! expr
                }
            |> TaskPort

        member _.Bind(m: TaskPort<'config,'a>, f: 'a -> TaskPort<'config, 'b>) = TaskPort.bind f m

        member _.Bind(m: TaskPort<'config,'a>, f: 'a -> Task<'b>) =
            TaskPort (fun env ->
                task {
                    let! x = TaskPort.run env m
                    return! f x
                }
            )

        member _.Bind(m: Port<'config,'a>, f: 'a -> TaskPort<'config,'b>) =
            TaskPort (fun env ->
                task {
                    let x = Port.run env m
                    return! TaskPort.run env (f x)
                }
            )

        member _.BindN(m1: Port<'config,'a1>, m2: Port<'config,'a2>, f: 'a1 * 'a2 -> TaskPort<'config,'b>) =
            TaskPort (fun env ->
                task {
                    let a1 = Port.run env m1
                    let a2 = Port.run env m2
                    return! TaskPort.run env (f (a1, a2))
                }
            )

        member _.Bind(m: Port<'config,'a>, f: 'a -> Task<'b>) =
            TaskPort (fun env ->
                task {
                    let x = Port.run env m
                    return! f x
                }
            )

        member _.Bind(m: Task<'a>, f: 'a -> TaskPort<'config,'b>) =
            TaskPort (fun env ->
                task {
                    let! x = m
                    return! TaskPort.run env (f x)
                }
            )
        member _.Bind(m: Task, f: unit -> TaskPort<'config,'a>) =
            TaskPort (fun env ->
                task {
                    do! m
                    return! TaskPort.run env (f ())
                }
            )

        member _.Bind(m: Task, f: unit -> Task<'a>) =
            TaskPort (fun _ ->
                task {
                    do! m
                    return! f ()
                }
            )

        member _.Bind(m: Task<'a>, f: 'a -> Task<'b>) =
            TaskPort (fun _ ->
                task {
                    let! x = m
                    return! f x
                }
            )

        member _.Zero() = TaskPort.retn ()

        member this.Combine(expr1: TaskPort<_,_>, expr2: TaskPort<_,_>) =
            this.Bind(expr1, (fun () -> expr2))

        member this.Combine(expr1: TaskPort<_,_>, expr2: 'a -> TaskPort<_,_>) =
            this.Bind(expr1, expr2)

        member _.Delay(func) = func
        member _.Run(delay) =
            TaskPort (fun env ->
                task {
                    let m = delay ()
                    return! TaskPort.run env m
                }
            )

        member _.Using(v, f) = TaskPort.using f v


    let taskPort = TaskPortBuilder()