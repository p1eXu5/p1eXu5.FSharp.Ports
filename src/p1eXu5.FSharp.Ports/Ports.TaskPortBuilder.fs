namespace p1eXu5.FSharp.Ports

open System.Threading.Tasks


type TaskPort<'config, 'a> = TaskPort of action: ('config -> Task<'a>)


module TaskPort =

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

    /// Map a function over a TaskPort
    let map f taskPort =
        TaskPort (fun env -> f (run env taskPort))

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


    let tee (f: 'a -> unit) (m: TaskPort<_,_>) =
        fun env ->
            task {
                let! x = run env m
                do
                    f x
                return x
            }
        |> TaskPort

    let apply (mf: TaskPort<'cfg, ('a -> 'b)>) (m: TaskPort<'cfg, 'a>) =
        fun env ->
            task {
                let! a = run env m
                let! f = run env mf
                return f a
            }
        |> TaskPort

    // ===============
    // Port
    // ===============

    let fromPort (expr: Port<_,_>) =
        fun env -> task { return Port.run env expr }
        |> TaskPort

    let fromPortF (f: 'a -> Port<_,_>) =
        fun _ -> task { return f }
        |> TaskPort

    let applyPort (m: TaskPort<'cfg, 'a>) (mf: TaskPort<'cfg, ('a -> Port<'cfg, 'b>)>) =
        fun env ->
            task {
                let! a = run env m
                let! f = run env mf
                let p = f a
                return Port.run env p
            }
        |> TaskPort

    // ===============
    // Task
    // ===============

    let fromTaskT (t: Task<_>) =
        fun _ -> task { return! t }
        |> TaskPort

    let fromTask (t: Task) =
        fun _ -> task { do! t }
        |> TaskPort

    let fromTaskF (f: 'a -> Task<'b>) =
        fun _ -> task { return f }
        |> TaskPort

    let applyTask (m: TaskPort<'cfg, 'a>) (mf: TaskPort<'cfg, ('a -> Task<'b>)>) =
        fun env ->
            task {
                let! a = run env m
                let! f = run env mf 
                return! f a
            }
        |> TaskPort

    // ===============
    // ValueTask
    // ===============

    let fromValueTaskT (t: ValueTask<_>) =
        fun _ -> task { return! t }
        |> TaskPort

    let fromValueTask (t: ValueTask) =
        fun _ -> task { do! t }
        |> TaskPort

    let fromValueTaskF (f: 'a -> ValueTask<'b>) =
        fun _ -> task { return f }
        |> TaskPort

    let applyValueTask (m: TaskPort<'cfg, 'a>) (mf: TaskPort<'cfg, ('a -> ValueTask<'b>)>) =
        fun env ->
            task {
                let! a = run env m
                let! f = run env mf 
                return! f a
            }
        |> TaskPort


open System

module TaskPortBuilderCE =

    type TaskPortBuilder () =
        member _.Zero() = TaskPort.retn ()

        member _.Return(v) = TaskPort.retn v
        
        member _.ReturnFrom(expr: TaskPort<_,_>) = expr
        member _.ReturnFrom(expr: Port<_,_>) = TaskPort.fromPort expr
        member _.ReturnFrom(expr: Task) = TaskPort.fromTask expr
        member _.ReturnFrom(expr: Task<_>) = TaskPort.fromTaskT expr
        member _.ReturnFrom(expr: ValueTask) = TaskPort.fromValueTask expr
        member _.ReturnFrom(expr: ValueTask<_>) = TaskPort.fromValueTaskT expr

        member    _.Bind(m: TaskPort<'config,'a>, f:   'a -> TaskPort<'config, 'b>) = TaskPort.bind f m
        member this.Bind(m: Port<'config,'a>,     f:   'a -> TaskPort<'config,'b>) = this.Bind(m |> TaskPort.fromPort, f)
        member this.Bind(m: Task<'a>,             f:   'a -> TaskPort<'config,'b>) = this.Bind(m |> TaskPort.fromTaskT, f)
        member this.Bind(m: ValueTask<'a>,        f:   'a -> TaskPort<'config,'b>) = this.Bind(m |> TaskPort.fromValueTaskT, f)
        member this.Bind(m: Task,                 f: unit -> TaskPort<'config,'a>) = this.Bind(m |> TaskPort.fromTask, f)
        member this.Bind(m: ValueTask,            f: unit -> TaskPort<'config,'a>) = this.Bind(m |> TaskPort.fromValueTask, f)

        member _.BindN(m1: Port<'config,'a1>, m2: Port<'config,'a2>, f: 'a1 * 'a2 -> TaskPort<'config,'b>) =
            TaskPort (fun env ->
                task {
                    let a1 = Port.run env m1
                    let a2 = Port.run env m2
                    return! TaskPort.run env (f (a1, a2))
                }
            )

        member this.Combine(expr1: TaskPort<_,_>, expr2: TaskPort<_,_>)       = this.Bind(expr1, (fun () -> expr2))
        member this.Combine(expr1: TaskPort<_,_>, expr2: 'a -> TaskPort<_,_>) = this.Bind(expr1, expr2)

        member _.Delay(func) = func
        member _.Run(delay) =
            TaskPort (fun env ->
                task {
                    let m = delay ()
                    return! TaskPort.run env m
                }
            )

        member this.TryWith(delayed: unit -> TaskPort<_,_>, handler: exn -> TaskPort<_,_>) =
            try
                this.ReturnFrom(delayed())
            with
                e ->
                    handler e

        member _.TryFinally(delayed: unit -> TaskPort<_,_>, compensation) =
            TaskPort (fun env ->
                task {
                    try
                        return! TaskPort.run env (delayed())
                    finally compensation()
                }
            )


        member this.Using(disposable:'a, body: 'a -> TaskPort<'b,'c>) =
            match box disposable with
            | :? IAsyncDisposable as disp ->
                TaskPort (fun env ->
                    task {
                        use! d = task { return disp }
                        return! TaskPort.run env (body disposable)
                    }
                )

            | :? IDisposable as disp ->
                let body' =
                    fun () -> body disposable

                this.TryFinally(body', fun () -> disp.Dispose ())

            | _ ->
                let body' =
                    fun () -> body disposable

                this.TryFinally(body', fun () -> ())

        member this.While(predicate, delayed: unit -> TaskPort<'conf, unit>) =
            if predicate () then
                this.Zero()
            else
                this.Bind( delayed (), fun () ->
                    this.While(predicate, delayed))

        member this.For(sequence:seq<_>, body) =
           this.Using(sequence.GetEnumerator(),fun enum ->
                this.While(enum.MoveNext,
                    this.Delay(fun () -> body enum.Current)))

    let taskPort = TaskPortBuilder()