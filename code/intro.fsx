#r "nuget: FsCheck, 2.16.6"
// Note that FsCheck 3 was released in January 2025, but there is no
// documentation for it available => for this demo we use FsCheck 2,
// even though only version 3 is currently maintained.

// This shows some strategies for choosing properties based on very simple toy problem statements.
// Example ideas (list soring/reversing) taken from https://fsharpforfunandprofit.com/posts/property-based-testing-3 Apache License 2.0 by Scott Wlaschin.

open FsCheck

let swap (arr: 'T[]) i j =
    let temp = arr.[i]
    arr.[i] <- arr.[j]
    arr.[j] <- temp

// AI generated, then manually introduced an error.
let rec quicksort (arr: 'T[]) left right =
    if left < right then
        let pivot = arr.[(left + right) / 2]
        let mutable i = left - 1
        let mutable j = right + 1

        let mutable continueLoop = true

        while continueLoop do
            i <- i + 1

            while arr.[i] < pivot do
                i <- i + 1

            j <- j - 1

            while arr.[j] > pivot do
                j <- j - 1

            if i >= j then continueLoop <- false else swap arr i j

        quicksort arr left (j - 1)
        quicksort arr (j + 1) right

let mysort (arr: list<int>) =
    let arr = List.toArray arr
    quicksort arr 0 (arr.Length - 1)
    List.ofArray arr

printfn "%A" (mysort [ 4; 3; 2; 1 ])
printfn "%A" (mysort [ 1; 2; 3; 4 ])
printfn "%A" (mysort [ 4; 2; 3; 9 ])
printfn "%A" (mysort [ 1; 1; 1; 1 ])
printfn "%A" (mysort [ -11; 10; -2000; 0; 0; 1 ])
printfn "%A" (mysort [])
printfn "%A" (mysort [ 0 ])
// Looks correct, doesn't it?

// Sorting doesn't change the number of elements in the list (invariant).
let propSortListNumberOfElements (xs: list<int>) : bool = xs.Length = (mysort xs).Length
Check.Quick propSortListNumberOfElements
Check.Verbose propSortListNumberOfElements

// Sorting doesn't change which elements are in the list (invariant).
let propSortListElements (xs: list<int>) =
    let a = xs |> Set.ofList
    let b = xs |> mysort |> Set.ofList
    a = b

Check.Quick propSortListElements

// Sorting a list twice is the same as sorting it once (idempotence).
let propSortListTwice (xs: list<int>) : bool =
    let a = mysort xs
    let b = mysort a
    a = b
// Other examples:
// - In general: fix point
// - Inserting element twice into set is same as inserting it once
// - Removing a file
// - Formatting code
// - Filtering a list by some condition

Check.Quick propSortListTwice

// Replay specific PRNG seed deterministically.
Check.One(
    { Config.Quick with
        Replay = Some(Random.StdGen(1299308234, 297581323)) },
    propSortListTwice
)
// Shrinking produces minimal example.

Check.One(
    { Config.Verbose with
        Replay = Some(Random.StdGen(1299308234, 297581323)) },
    propSortListTwice
)

printfn "%A" (mysort [ 1; 2; 0 ])
printfn "%A" (mysort (mysort [ 1; 2; 0 ])) // What's that?

// Fix mysort off-by-one error.
// quicksort arr left j

// "Hard" to find a solution (sort a list), but easy to verify (all pairs are ordered).
let sortListPairwiseOrdered (xs: list<int>) : bool =
    xs |> mysort |> List.pairwise |> List.forall (fun (a, b) -> a <= b)
// Note: e.g. let sort xs = [0] would satisfy that.
// But together with sortListNumberOfElements this is excluded.

Check.Quick sortListPairwiseOrdered

// Testing oracle / second implementation: An inefficient but
// more obviously correct sorting implementation.
let bubbleSort (input: list<'T>) =
    let arr = Array.ofList input

    let swap i j =
        let tmp = arr.[i]
        arr.[i] <- arr.[j]
        arr.[j] <- tmp

    let mutable swapped = true

    while swapped do
        swapped <- false

        for i = 1 to arr.Length - 1 do
            if arr.[i - 1] > arr.[i] then
                swap (i - 1) i
                swapped <- true

    List.ofArray arr

let propSortOracleDifferential (xs: list<int>) =
    let a = bubbleSort xs
    let b = mysort xs
    a = b

Check.Quick propSortOracleDifferential

// Inverses / round-trip: Reversing a list twice yields the original list.
let propRevRevIsOrig (xs: list<int>) = List.rev (List.rev xs) = xs
Check.Quick propRevRevIsOrig
Check.Verbose propRevRevIsOrig

let propRevRevIsOrigFloat (xs: list<float>) = List.rev (List.rev xs) = xs
Check.Quick propRevRevIsOrigFloat
