// SPDX-FileCopyrightText: 2026 Stefan Walter (stfnw)
//
// SPDX-License-Identifier: MIT

// Variation of a scheduling/bin-packing problem; problem statement:
// Given a list of tasks, each of which takes some time to complete,
// and a list of machines, each with a given time capacity available,
// does there exist a valid schedule (mapping tasks to machines)
// such that all tasks get completed, and each machines' capacity
// is not exceeded.

type Task = { processing_time: int }
type Machine = { capacity_time: int }
type Schedule = list<int * int> // Assignment/mapping from TaskIds to MachineIds.

// Inefficient brute-force backtracking implementation (partly AI generated).
let findSchedule (tasks: list<Task>) (machines: list<Machine>) : option<Schedule> =
    let machines = machines |> List.map (fun m -> m.capacity_time) |> List.toArray

    let indexedTasks: list<int * Task> = tasks |> List.indexed

    let rec backtrack (taskIdx: int) (machineCapacities: array<int>) : option<Schedule> =
        // Base case: All tasks assigned
        if taskIdx >= List.length indexedTasks then
            Some []
        else
            let taskId, task = indexedTasks.[taskIdx]

            // Try assigning the current task to each machine.
            let rec tryMachines (machineId: int) : option<Schedule> =
                if machineId >= Array.length machineCapacities then
                    None // No machine can fit this task in this branch.
                else if machineCapacities.[machineId] >= task.processing_time then
                    // Assign task to this machine
                    machineCapacities.[machineId] <- machineCapacities.[machineId] - task.processing_time

                    match backtrack (taskIdx + 1) machineCapacities with
                    | Some remainingAssignments -> Some((taskId, machineId) :: remainingAssignments)
                    | None ->
                        // Backtrack: Undo assignment and try next machine.
                        machineCapacities.[machineId] <- machineCapacities.[machineId] + task.processing_time
                        tryMachines (machineId + 1)
                else
                    // Machine cannot fit task, try next machine.
                    tryMachines (machineId + 1)

            tryMachines 0

    backtrack 0 machines

////////////////////////////////////////////////////////////////////////////////
// Some specific examples. /////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

type Example =
    { tasks: list<Task>
      machines: list<Machine> }

let example1: Example =
    { tasks =
        [ { processing_time = 7 }
          { processing_time = 2 }
          { processing_time = 3 }
          { processing_time = 4 }
          { processing_time = 1 } ]
      machines = [ { capacity_time = 10 }; { capacity_time = 13 }; { capacity_time = 7 } ] }

let solution = findSchedule example1.tasks example1.machines

let example2: Example =
    { tasks =
        [ { processing_time =  99 }
          { processing_time = 100 }
          { processing_time = 3 } ]
      machines = [ { capacity_time = 5 }; { capacity_time = 8 } ] }

printfn "%A" (findSchedule example2.tasks example2.machines)

////////////////////////////////////////////////////////////////////////////////
// Example of a property: verification of the output of findSchedule is easy. //
////////////////////////////////////////////////////////////////////////////////

let isValidSchedule (tasks: list<Task>) (machines: list<Machine>) (schedule: Schedule) : bool =
    if tasks.IsEmpty then
        true // An empty  list of tasks can always be scheduled.
    else
        // All tasks must be assigned/completed.
        let taskIdxsTasks = [ 0 .. (tasks.Length - 1) ] |> Set.ofList
        let taskIdxsSchedule = schedule |> List.map fst |> Set.ofList
        let allTasksAssigned = taskIdxsTasks = taskIdxsSchedule

        // All machines must be within their capacity.
        let allMachinesWithinCapacity =
            let scheduleGroupedByMachineIdx = schedule |> List.groupBy snd

            scheduleGroupedByMachineIdx
            |> List.map (fun (machineIdx, assignments) ->
                let taskIdxs = List.map fst assignments

                if machineIdx < 0 || machineIdx >= machines.Length then
                    false
                elif taskIdxs |> List.exists (fun i -> i < 0 || i >= tasks.Length) then
                    false
                else
                    let capacitySum =
                        taskIdxs |> List.map (fun i -> tasks.[i].processing_time) |> List.sum

                    capacitySum <= machines.[machineIdx].capacity_time)
            |> List.reduce (&&)

        allTasksAssigned && allMachinesWithinCapacity


printfn
    "%A"
    (solution
     |> Option.iter (isValidSchedule example1.tasks example1.machines >> printfn "%A"))

////////////////////////////////////////////////////////////////////////////////
// Example of how to integrate it into property testing library FsCheck. ///////
////////////////////////////////////////////////////////////////////////////////

#r "nuget: FsCheck, 2.16.6"

open FsCheck

module TaskGen =
    // Generate random tasks.
    let taskGen: Gen<Task> =
        Gen.choose (1, 10) |> Gen.map (fun i -> { processing_time = i })

    type MyGenerator =
        static member Task() = Arb.fromGen taskGen

    // Register this generator globally so that we can do
    // type-inferred autogeneration.
    Arb.register<MyGenerator> () |> ignore

printf "%A" (Gen.sample 10 10 Arb.generate<Task>)

module MachineGen =
    // Generate random machines.
    let machineGen: Gen<Machine> =
        Gen.choose (1, 10) |> Gen.map (fun i -> { capacity_time = i })

    type MyGenerator =
        static member Machine() = Arb.fromGen machineGen

    Arb.register<MyGenerator> () |> ignore

printf "%A" (Gen.sample 10 10 Arb.generate<Machine>)

module ExampleGen =
    // Generate a random problem input (list of tasks and list of machines).
    let exampleGen: Gen<Example> =
        let tasks: Gen<list<Task>> = TaskGen.taskGen |> Gen.listOf
        let machines: Gen<list<Machine>> = MachineGen.machineGen |> Gen.listOf
        Gen.map2 (fun ts ms -> { tasks = ts; machines = ms }) tasks machines

    type MyGenerator =
        static member Example() = Arb.fromGen exampleGen

    Arb.register<MyGenerator> () |> ignore

printf "%A" (Gen.sample 3 5 Arb.generate<Example>)



// Check that for a sample of randomly generated example inputs,
// schedules returned by findSchedule are indeed valid.
let propCapacityValid (input: Example) : Property =
    let solution = findSchedule input.tasks input.machines

    match solution with
    | Some solution -> isValidSchedule input.tasks input.machines solution |> Prop.ofTestable
    | None -> Prop.discard () // Ignore this case.


// Check.Quick propCapacityValid
Check.One({ Config.Quick with EndSize = 7 }, propCapacityValid)

// Lots of other possible properties, here are some:
// Same task shouldn't be assigned twice in a schedule.
// Adding a new machine with capacity zero shouldn't change the result.
// (Sorted) schedule should be independent from order of task/machine lists.
// If I have two inputs for which a schedule exists, merging inputs should also yield a valid schedule.
// Smarter generators for always valid schedules / go the other way in input construction: start from machines, randomly subdivide each of their capacity into chunks of tasks, shuffle the chunks => that is the input.
// Test-oracle: differentially test a proper efficient implementation against the simple backtracking one.
// ...
