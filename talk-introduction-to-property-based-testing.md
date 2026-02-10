---
marp: true
paginate: true
title: "Introduction to property based testing"
subtitle: "Tü.λ—Functional Programming Night Tübingen"
author: "Stefan Walter"
date: "2026-01-28"
style: |
    section {
        align-content: start;
    }

---

<!--
SPDX-FileCopyrightText: 2026 Stefan Walter (stfnw)

SPDX-License-Identifier: CC-BY-4.0
-->

<!--
rahmenbedingungen
- ca 45 min vortrag 15 min diskussion
- display beamer hdmi
-->

# Introduction to property based testing

Tü.λ—Functional Programming Night Tübingen

*2026-01-28* Stefan Walter

Slides and code: https://github.com/stfnw/talk-introduction-to-property-based-testing

---

<!-- Talk proposal: Introduction to Property-Based Testing

How can we test our software, find bugs and make sure that it works as intended -- without time-consumingly hand-crafting example-based tests, which are still likely to miss important cases?

Originating from Haskell's QuickCheck and the world of functional programming, *property-based testing* is one interesting approach to this problem:
instead of writing individual tests, inputs are automatically generated and the implementation is verified by checking expected relationships between inputs and outputs (*properties*).

This introduction will cover the basics:
Starting from testing purely functional code, we'll discuss strategies for expressing properties without actually knowing the correct solution beforehand and without having to duplicate implementation logic.
This will be most useful during development.
From there on, we'll also take a look into model-based/stateful property tests, which can be used when you are not in full control of the system-under-test, e.g. for testing non-functional stateful code/interfaces and already-existing real-world systems.

Afterwards I'd like to hear from *YOU*:
- When did you use property testing?
- What was a surprising or interesting property that helped you specify system behavior?
- Can you share further strategies for expressing properties?
-->

# Motivation

Testing: Correctness, Safety
<!--
- critical applications
- gain confidence

to set expectations
- i'm not mainly a developer
- this is in aggregation of information and examples from others
- mixed with in part practical application
- will include list of sources and further resources at the end
-->

---

# Outline

- *pure* code
- *stateful* code
- some case studies

<!--
---

# Code samples

- here in F# (Microsoft)
- functional language (expression-based, immutable by default, first-class functions, currying, static strong typing type inference Hindley-Milner type system, ADTs)
- backed by .NET platform
- good resource: https://fsharpforfunandprofit.com
-->

---

# Introductory example

- sorting lists of integers
- `intro.fsx`

<!--
- typical example
- easy problem
- to make it less abstract and get a feel for how this could look in practice, that show how to apply this to very easy problems.
- we will use those to explore some general approaches.
- later, we will look at more complex/real examples.

- FsCheck: property testing library for F#
- nice combinator api
-->

---

# Example based tests

manually hand-code specific examples

---

## Advantages

- easy to write and understand (concrete/specific)
- specific edge cases
- regression

---

## Disadvantages

- small number of tests
- limited coverage: Hard to think of representative data covering all cases
- miss edge cases, non-exhaustive

---

# Property based tests

- originally introduced from functional programming by John Hughes and Koen Claessen with the QuickCheck library for Haskell
- randomly auto-generate tests / test data, and check *properties* of the implementation
- i.e. exptected relationship between inputs and outputs

---

## Advantages

- huge number of tests
- can cover more code paths / higher code coverage
- can uncover unexpected test cases

---

## Disadvantages

- more abstract
- harder to write
- still not exhaustive (not formal verification); may still miss edge cases
<!-- Fuzzing -->

---

## How to specify desired properties?

*without* re-implementing the system-under-test / duplicating logic

<!--
- How to determine correct behavior **without already having a solution of the problem**?
- Is there anything we can say about the result and its relationship to the input, without actually knowing what the result should be?
-->

---

## Strategies for systematically specifing properties

<!-- Aggregation of external resources / examples -->

good (re)sources:

- Scott Wlaschin [The "Property Based Testing" series](https://fsharpforfunandprofit.com/series/property-based-testing)
- John Hughes [How to specify it! A guide to writing properties of pure functions | Code Mesh LDN 19](https://www.youtube.com/watch?v=zvRAyq5wj38)
  J. Hughes, “How to Specify It!: A Guide to Writing Properties of Pure Functions,” in Trends in Functional Programming, vol. 12053, W. J. Bowman and R. Garcia, Eds., in Lecture Notes in Computer Science, vol. 12053. , Cham: Springer International Publishing, 2020, pp. 58–83. doi: 10.1007/978-3-030-47147-7_4.

---

### Invariants

something stays unchanged

---

### Idempotence

doing something twice is the same as doing it once

---

### Hard to solve, easy to verify

- e.g. NP complete problems
- example: scheduling / resource allocation, `scheduling.fsx`
    - *verification* of output of `findSchedule` is easy
    - if sum of task times $>$ sum of machine capacities: no valid schedule exists

<!--
- many variations / related problems: cpu scheduling, assigning jobs to machines, assigning shifts to employees, Knapsack problem, bin packing, 3-partition problem, ...
- As we will see later, good example for suitable problem statement, way of stating the problem
- Hard to solve, easy to verify
- much code, not needed to read in detail; won't actually find bugs here in the implementation, just as an example
-->

<!--
- prime number factorizations: check that returned numbers multiplied together give the input
- sort: each pair of adjacent elements is ordered
-->

---

### Inverse / round-trip

- compression - decompression
- serialization - deserialization
- write - read
- addition - subtraction
- encode - decode
<!-- Example from the python standard library: b64 encode decode round-trip:
https://github.com/python/cpython/blob/6ea3f8cd7ff4ff9769ae276dabc0753e09dca998/Lib/test/test_base64.py#L424
-->
- render - parse
- do - undo - redo
<!--
- UI: menu navigation on tv: press sequence of arrows x times (as long as not on edge), press inverse x times, should result in same position
-->

<!--
---

### Postconditions

what should be true after calling some function?
-->

---

### Divide into subproblems

relate problem statement to a smaller subproblem

- divide and conquer algorithms
- structural induction

---

### Metamorphic properties

- how does modifying the input change/affect the output? (relationship)
<!--
given an action op1, how does the output of op1(input) relate to the output of a modified input op2(input)? (how do op1(X) or op1(op2(X)) and op1(op2(X)) relate)
-->
- combine operations

---

### Metamorphic properties examples

- search engine: [^1]
  when a search yields result $A$
  a constrained search (e.g. with date filter) yields result $B$
  then $B \subseteq A$

- image recognition of position of specific object: <!-- don't know the source of this example anymore -->
    - rotate input image, also rotates output position
    - changing color to grayscale shouldn't affect result

- filter then sort == sort then filter

- negate all numbers in list then sort == sort list then negate all numbers

<!-- _footer: 1: From [Automate Your Way to Better Code: Advanced Property Testing (with Oskar Wickström)](https://www.youtube.com/watch?v=wHJZ0icwSkc) -->

---

### Test oracle / differential testing

second implementation that solves problem

- legacy system is oracle for replacement system
- compare multiple existing implementations of a standard

---

### Model-based properties

special case of test oracle where you provide the model as one implementation:
- re-implement aspect of functionality you care about in a simpler way
- differentially test against that

example use cases:
- optimized complicated implementation vs slow but simpler (more obviously correct) implementation
- parallel vs single threaded implementation
- in-memory model vs multi-system application deployment

<!--
John Hughes gives example of binary tree with operations, modeled with lists
-->

---

## Strategies for systematically specifing properties summary

- (doesn't crash)
<!-- Fuzzing -->

- Invariants <!-- something stays unchanged -->
- Idempotence <!-- doing something twice is the same as doing it once -->
- Hard to solve, easy to verify
- Inverse / round-trip
- Divide into subproblems
- Metamorphic properties <!-- how does modifying the input change/affect the output? -->
- Test oracle / differential testing
- Model-based properties

<!-- typical mathematical properties
- Associativity: Grouping doesn't matter
- Commutativity: Order doesn't matter
- Neutral/identity element
-->

---

### Efficiency

recommendations from overview of strategies, test against example bugs: [^1]
<!-- _footer: 1: From Chapter 5.3, J. Hughes, “How to Specify It!: A Guide to Writing Properties of Pure Functions,” in Trends in Functional Programming, vol. 12053, W. J. Bowman and R. Garcia, Eds., in Lecture Notes in Computer Science, vol. 12053. , Cham: Springer International Publishing, 2020, pp. 58–83. doi: 10.1007/978-3-030-47147-7_4. -->

- model-based
<!-- Complete specification, few properties required, but duplicates logic -->
- metamorphic
<!-- Don't require model, huge number of combinations, n operations -> n^2 properties -->

<!--
---

## Important parts

- Generators
- Shrinkers
- Preconditions

- Generate input data from pseudo-random number generator and size
- Shrink found falsifying examples down to minimal example.
- Precondition/filter vs smarter generators.
-->

---

# Outline

- *pure* code
- *stateful* code
- some case studies

---

# Property testing of *purely functional* code

- no side effects, no implicit state (stateless)
- mathematical function: same input $\Rightarrow$ same output
- i.e. everything covered so far

most useful:
- in functional programming with adequate language support (immutability)
- defense, when you are developer in control of the code

---

# Property testing of *stateful* code

- side effects and external state
- dynamically created resources

<!--
- interaction with file system: open a file => resource handles, file descriptors
  write to fd, read from fd
- ringbuffer, where creation of ringbuffer itself with different parameters should be tested
  create(size), push(item), pop
-->

most useful:
- test of real-world / existing stateful systems
- interface / api testing
- offense, finding bugs end-to-end

<!--
Coming from property testing, this is very much an edge case and not all property testing libraries support it.
Goes much more in the general direction of fuzzing.
-->

---

## Typical approach/strategy

**model-based**

- re-implement relevant aspect of system-under-test in a model
  has internal state and supports operations on it
- generate a random list of function calls / actions (program)
- execute each action in the program against real-world system *and* model
- assert that results for each are the same

---

## Example: filesystem

pseudo-code interface:

- `open(path: string) -> fd`:
  Open "path", return opaque symbolic resource handle identifying the open file
  (file descriptor / handle).

- `readall(fd, size: int) -> [u8]`
  Read "size" bytes from an opened file, return corresponding byte buffer.

- `writeall(fd, buf: [u8]) -> ()`
  Write byte buffer, return unit (no return value).

---

## Example list of actions against a filesystem

```
fd0 = open("/tmp/path")
writeall(fd0, "hello world")
fd1 = open("/tmp/path")
readall(fd1, 11)
```

<!-- Note: this is only the program representation,
nothing has been run / actually executed yet. The fds are symbolic
handles/resources for referring back to the result that will actually be
returned during actual execution

```c
// clear ; gcc -Wall -Wpedantic -Werror -g -o test test.c
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>

int main() {
  {
    int f = open("/tmp/path", O_RDWR | O_CREAT, 0644);
    char buf[] = "hello world";
    write(f, buf, sizeof(buf));
  }

  {
    int f = open("/tmp/path", O_RDWR | O_CREAT, 0644);
    char buf[0x100];
    read(f, buf, sizeof(buf));
    printf("%s", buf);
  }

  return 0;
}
```
-->

---

## Example execution

against real file system:

```
open("/tmp/path")               -> 3
writeall(fd0, "hello world")    -> ()
open("/tmp/path")               -> 4
readall(fd0, 11)                -> "hello world"
```

against a correct model:

```
open("/tmp/path")               -> FD0
writeall(fd0, "hello world")    -> ()
open("/tmp/path")               -> FD1
readall(fd0, 11)                -> "hello world"
```

---

## Is a model actually necessary?

<!-- duplicate code/implementation, for pure code not necessarily the first choice -->

Yes! drives test generation.

<!--
$\Rightarrow$ test generation (choice of valid actions to take) itself is driven by model and depends on current state / history.
(e.g. can't read from a file that hasn't been opened yet).
-->

---

## Disadvantages

- needs model as runnable specification
- duplicate implementation

---

## Advantages

- can test real-world systems
- grey-box testing, only interface must be known

<!--
Can test even physical ones. Assuming you have some device that exposes a hardware interface (some buttons), and you absolutely can't change that or interface with it in another way by e.g. simply soldering on wires,
nothing stops you from writing a model for that, hooking it up to a robot arm, and running automated tests in the actual physical world.
-->

---

# Some case studies

- *pure:* URI parsing in .NET
- *stateful:* Reproducing CVE-2022-0847 (Dirty Pipe)
  (a security-relevant logic bug in the Linux kernel)

---

## URI parsing in .NET

`uri-parsing.fsx`

---

## Reproducing CVE-2022-0847 (Dirty Pipe)

- a security-relevant logic bug in the Linux kernel
- https://dirtypipe.cm4all.com/
- found by Max Kellermann

---

### Gist

- in certain cases permissions are not checked and one can write to a file one shouldn't be able to
- can be used for privilege escalation
<!-- No time to go into details how it works; and are not particularly relevant. -->
- ultimately caused by an *optimization*
<!-- Model-based useful: Differential testing against simpler implementation. -->

---

### Example trace of actions during exploit

(output from strace shows syscalls and parameters during run as unprivileged user)

```strace
openat(AT_FDCWD, "/etc/passwd", O_RDONLY)            = 3
pipe2([4, 5], 0)                                     = 0
fcntl(5, F_GETPIPE_SZ)                               = 65536
write(5, "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"..., 65536) = 65536
read(4, "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\"..., 65536) = 65536
splice(3, [0], 5, NULL, 1, 0)                        = 1
write(5, "AAAA", 4)                                  = 4
read(3, "rAAAAx:0", 8)                               = 8
```

last read shouldn't return written data! (file opened read-only)
<!-- regardless of all the rest happening (ignore that),
     the sequence of first and last syscall should not be possible as regular user . -->

---

### Reproducer

- I wrote a poc reproducer for it
- https://github.com/stfnw/reproducer-poc-CVE-2022-0847
- generates actions (syscalls) with appropriate parameters

- detects misbehavior by running actions against real-world file system and a model
- warning: lots of magic numbers / chosen parameters, not *fully* realistic,
  state space too large to traverse in acceptable time without guidance/feedback

---

### Demo

---

### Lessons learned

- implementing a model is useful for understanding interface better
- model gets more complicated than expected very quickly
- handling all error conditions and making sure generators only produce valid commands that don't mess up implicit program state is surprisingly hard

---

# Mildly related

<!-- testing, software correctness in general, stuff i also found interesting -->

- more aspects of model-based stateful testing:
  parallel testing, concurrency issues, distributed systems (test interleavings)

- natural next step for vulnerability search: combine with modern fuzzing
    - mutation based, coverage guided
    - use properties as crash-oracle for fuzzing for high-level logic bugs
  *$\Rightarrow$ I'm interested in that; you too? Let me know!*

- (lightweight) formal methods

- formal specifications and model checking / ltl / tla+ / alloy

- deterministic simulation testing (TigerBeetle, Antithesis)

---

# Sources and further resources

<!-- various media text/video/audio/code -->

- Scott Wlaschin:
    - [The "Property Based Testing" series](https://fsharpforfunandprofit.com/series/property-based-testing); especially sections on choosing properties in practice
    - [F# Online - Scott Wlaschin - Property Based Testing](https://www.youtube.com/watch?v=99oO-6EIyck)
    - [The lazy programmer's guide to writing thousands of tests - Scott Wlaschin](https://www.youtube.com/watch?v=IYzDFHx6QPY)

- J. Hughes, “How to Specify It!: A Guide to Writing Properties of Pure Functions,” in Trends in Functional Programming, vol. 12053, W. J. Bowman and R. Garcia, Eds., in Lecture Notes in Computer Science, vol. 12053. , Cham: Springer International Publishing, 2020, pp. 58–83. doi: 10.1007/978-3-030-47147-7_4.
- [John Hughes - How to specify it! A guide to writing properties of pure functions | Code Mesh LDN 19](https://www.youtube.com/watch?v=zvRAyq5wj38)

---

# Sources and further resources

- QuickCheck:
    - K. Claessen and J. Hughes, “QuickCheck: a lightweight tool for random testing of Haskell programs,” in Proceedings of the fifth ACM SIGPLAN international conference on Functional programming, ACM, Sep. 2000, pp. 268–279. doi: 10.1145/351240.351266.
    - K. Claessen and J. Hughes, “Testing monadic code with QuickCheck,” in Proceedings of the 2002 ACM SIGPLAN workshop on Haskell, Pittsburgh Pennsylvania: ACM, Oct. 2002, pp. 65–77. doi: 10.1145/581690.581696.
    - https://begriffs.com/posts/2017-01-14-design-use-quickcheck.html

---

# Sources and further resources

- F# property based testing library [FsCheck](https://fscheck.github.io/FsCheck)

- Python property based testing library with good documentation and novel shrinking strategy: [Hypothesis](https://hypothesis.readthedocs.io/en/latest)
  Paper on internal shrinking used by Pythons Hypothesis: D. R. MacIver and A. F. Donaldson, “Test-Case Reduction via Test-Case Generation: Insights from the Hypothesis Reducer (Tool Insights Paper),” LIPIcs, Volume 166, ECOOP 2020, vol. 166, p. 13:1-13:27, 2020, doi: 10.4230/LIPICS.ECOOP.2020.13.

---

# Sources and further resources

- [The sad state of property-based testing libraries](https://stevana.github.io/the_sad_state_of_property-based_testing_libraries.html)
- Podcast "Developer Voices" by Kris Jenkins (interviews/case studies):
    - [Automate Your Way to Better Code: Advanced Property Testing (with Oskar Wickström)](https://www.youtube.com/watch?v=wHJZ0icwSkc)
    - [From Unit Tests to Whole Universe Tests (with Will Wilson)](https://www.youtube.com/watch?v=_xJ4maWhSNU)
- [Property-Based Testing in a Screencast Editor: Introduction](https://wickstrom.tech/2019-03-02-property-based-testing-in-a-screencast-editor-introduction.html) by Oskar Wickström
- [Race Conditions, Distribution, Interactions Testing the Hard Stuff and Staying Sane](https://vimeo.com/68383317) by John Hughes
- [Property Based Testing: Concepts and Examples - Kenneth Kousen](https://www.youtube.com/watch?v=TWxI5FXAae0)

---

# Sources and further resources
- Code samples
    - https://github.com/kousen/pbt_jqwik
    - https://github.com/swlaschin/PropBasedTestingTalk

---

# Questions and discussion

- When did you use property testing?
- What was a surprising or interesting property that helped you specify system behavior?
- Can you share further strategies for expressing properties?
