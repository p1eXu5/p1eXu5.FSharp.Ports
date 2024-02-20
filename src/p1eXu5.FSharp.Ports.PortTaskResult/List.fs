namespace p1eXu5.FSharp.Ports

[<RequireQualifiedAccess>]
module List =

    let traversePortTaskResultA (f: 'a -> PortTaskResult<'env,'b,'err>) (xs: 'a list) =
        let retn = PortTaskResult.retn
        let (initState: PortTaskResult<'env, 'b list, 'err list>) = retn []

        let folder head (tail: PortTaskResult<_,_,_>) : PortTaskResult<_,_,_> =
            fun env ->
                task {
                    let! t = tail |> PortTask.run env
                    let! h = f head |> PortTask.run env
                    match t, h with
                    | Error terr, Ok _ ->
                        return Error terr
                    | Error terr, Error herr ->
                        return Error (herr :: terr)
                    | Ok tok, Ok hok ->
                        return Ok (hok :: tok)
                    | Ok _, Error herr ->
                        return Error [herr]
                }
            |> Port

        List.foldBack folder xs initState


    let traversePortTaskResultM (f: 'a -> PortTaskResult<'env,'b,'err>) (xs: 'a list) =
        let (>>=) x f = PortTaskResult.bind f x
        let retn = PortTaskResult.retn

        let cons head tail = head :: tail
        let initState = retn []

        let folder head tail =
            f head >>= (fun h ->
                tail >>= (fun t ->
                    retn (cons h t)
                )
            )

        List.foldBack folder xs initState


    let sequencePortTaskResultM (ports: PortTaskResult<'env,'a,'err> list) =
        traversePortTaskResultM id ports

    let sequencePortTaskResultA (ports: PortTaskResult<'env,'a,'err> list) =
        traversePortTaskResultA id ports