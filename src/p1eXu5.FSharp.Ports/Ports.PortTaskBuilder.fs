namespace p1eXu5.FSharp.Ports

open System.Threading.Tasks


type PortTask<'env, 'a> = Port<'env, Task<'a>>


module PortTask =

    /// Run a TaskPort with a given environment
    let run env (portTask: PortTask<_,_>) = Port.run env portTask

    let runSynchronously env (portTask: PortTask<'env, 'a>)  =
        let (Port action) = portTask
        action env
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let retn v : PortTask<_,_> = (fun _ -> task { return v }) |> Port

    /// Create a TaskPort which returns the environment itself
    let ask : PortTask<_,_> = Port (fun env -> task { return env })

    /// Map a function over a TaskPort
    let map f portTask : PortTask<_,_> =
        Port (fun env ->
            task {
                let! a = run env portTask
                return f a
            }
        )

    let bindT taskf portTask : PortTask<_,_> =
        Port (fun env ->
            task {
                let! a = run env portTask
                return! taskf a
            }
        )

    /// flatMap a function over a TaskPort
    let bind portTaskf portTask : PortTask<_,_> =
        let newAction env =
            task {
                let! x = run env portTask
                return! run env (portTaskf x)
            }
        Port newAction

    let withEnv f (portTask: PortTask<_,_>) : PortTask<_,_> =
        Port (fun env ->
            task {
                return! run (f env) portTask
            }
        )

    let tee (f: 'a -> unit) (portTask: PortTask<_,_>) : PortTask<_,_> =
        fun env ->
            task {
                let! x = run env portTask
                do
                    f x
                return x
            }
        |> Port

    let apply (mf: PortTask<'cfg, ('a -> 'b)>) (portTask: PortTask<'cfg, 'a>) : PortTask<_,_> =
        fun env ->
            task {
                let! a = run env portTask
                let! f = run env mf
                return f a
            }
        |> Port

    // ===============
    // Port
    // ===============

    let fromPort (port: Port<_,_>) : PortTask<_,_> =
        fun env -> task { return Port.run env port }
        |> Port

    let fromPortF (f: 'a -> Port<_,_>) : PortTask<_,_> =
        fun _ -> task { return f }
        |> Port

    let applyPort (portTask: PortTask<'cfg, 'a>) (mf: PortTask<'cfg, ('a -> Port<'cfg, 'b>)>) : PortTask<_,_> =
        fun env ->
            task {
                let! a = run env portTask
                let! f = run env mf
                let p = f a
                return Port.run env p
            }
        |> Port

    // ===============
    // Task
    // ===============

    let fromTaskT (t: Task<_>) : PortTask<_,_> =
        fun _ -> task { return! t }
        |> Port

    let fromTask (t: Task) : PortTask<_,_> =
        fun _ -> task { do! t }
        |> Port

    let fromTaskF (f: 'a -> Task<'b>) : PortTask<_,_> =
        fun _ -> task { return f }
        |> Port

    let applyTask (portTask: PortTask<'cfg, 'a>) (portTaskf: PortTask<'cfg, ('a -> Task<'b>)>) : PortTask<_,_> =
        fun env ->
            task {
                let! a = run env portTask
                let! f = run env portTaskf 
                return! f a
            }
        |> Port

    // ===============
    // ValueTask
    // ===============

    let fromValueTaskT (t: ValueTask<_>) : PortTask<_,_> =
        fun _ -> task { return! t }
        |> Port

    let fromValueTask (t: ValueTask) : PortTask<_,_> =
        fun _ -> task { do! t }
        |> Port

    let fromValueTaskF (f: 'a -> ValueTask<'b>) : PortTask<_,_> =
        fun _ -> task { return f }
        |> Port

    let applyValueTask (portTask: PortTask<'cfg, 'a>) (portTaskF: PortTask<'cfg, ('a -> ValueTask<'b>)>) : PortTask<_,_> =
        fun env ->
            task {
                let! a = run env portTask
                let! f = run env portTaskF 
                return! f a
            }
        |> Port


open System

module PortTaskBuilderCE =

    type PortTaskBuilder () =
        member _.Zero() = PortTask.retn ()

        member _.Return(v) = PortTask.retn v

        member _.ReturnFrom(expr: PortTask<_,_>) = expr
        member _.ReturnFrom(expr: Task) = PortTask.fromTask expr
        member _.ReturnFrom(expr: Task<_>) = PortTask.fromTaskT expr
        member _.ReturnFrom(expr: ValueTask) = PortTask.fromValueTask expr
        member _.ReturnFrom(expr: ValueTask<_>) = PortTask.fromValueTaskT expr

        member    _.Bind(m: PortTask<'env,'a>, f:   'a -> PortTask<'env, 'b>) = PortTask.bind f m
        member this.Bind(m: Task<'a>,             f:   'a -> PortTask<'env,'b>) = this.Bind(m |> PortTask.fromTaskT, f)
        member this.Bind(m: ValueTask<'a>,        f:   'a -> PortTask<'env,'b>) = this.Bind(m |> PortTask.fromValueTaskT, f)
        member this.Bind(m: Task,                 f: unit -> PortTask<'env,'a>) = this.Bind(m |> PortTask.fromTask, f)
        member this.Bind(m: ValueTask,            f: unit -> PortTask<'env,'a>) = this.Bind(m |> PortTask.fromValueTask, f)

        member _.BindN(m1: Port<'env,'a1>, m2: Port<'env,'a2>, f: 'a1 * 'a2 -> PortTask<'env,'b>) =
            Port (fun env ->
                task {
                    let a1 = Port.run env m1
                    let a2 = Port.run env m2
                    return! PortTask.run env (f (a1, a2))
                }
            )

        member this.Combine(expr1: PortTask<_,_>, expr2: PortTask<_,_>)       = this.Bind(expr1, (fun () -> expr2))
        member this.Combine(expr1: PortTask<_,_>, expr2: 'a -> PortTask<_,_>) = this.Bind(expr1, expr2)

        member _.Delay(func) = func
        member _.Run(delay: unit -> PortTask<_,_>) =
            Port (fun env ->
                task {
                    let m = delay ()
                    return! PortTask.run env m
                }
            )

        member this.TryWith(delayed: unit -> PortTask<_,_>, handler: exn -> PortTask<_,_>) =
            try
                this.ReturnFrom(delayed())
            with
                e ->
                    handler e


        member _.TryFinally(delayed: unit -> PortTask<_,_>, compensation) =
            Port (fun env ->
                task {
                    try
                        return! PortTask.run env (delayed())
                    finally compensation()
                }
            )


        member this.Using(disposable:'a, body: 'a -> PortTask<'b,'c>) =
            match box disposable with
            | :? IAsyncDisposable as disp ->
                Port (fun env ->
                    task {
                        use! d = task { return disp }
                        return! PortTask.run env (body disposable)
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


        member this.While(predicate, delayed: unit -> PortTask<'env, unit>) =
            if predicate () then
                this.Zero()
            else
                this.Bind( delayed (), fun () ->
                    this.While(predicate, delayed))


        member this.For(sequence:seq<_>, body) =
           this.Using(sequence.GetEnumerator(),fun enum ->
                this.While(enum.MoveNext,
                    this.Delay(fun () -> body enum.Current)))

    let portTask = PortTaskBuilder()