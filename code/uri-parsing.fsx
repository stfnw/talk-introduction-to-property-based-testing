// SPDX-FileCopyrightText: 2026 Stefan Walter (stfnw)
//
// SPDX-License-Identifier: MIT

#r "nuget: FsCheck, 2.16.6"

open FsCheck

// Test some very limited aspects of URI parsing in .NET framework.
// https://learn.microsoft.com/en-us/dotnet/api/system.uri?view=net-10.0

// Look at the source: https://github.com/dotnet/runtime

// Property based testing_seems to be used only in System.Formats.Cbor
// $ grep -ri fscheck -l | sort
// eng/Versions.props
// src/libraries/System.Formats.Cbor/tests/CborDocument/CborDocument.fs
// src/libraries/System.Formats.Cbor/tests/PropertyTests/CborPropertyTests.cs
// src/libraries/System.Formats.Cbor/tests/PropertyTests/CborRandomGenerators.cs
// src/libraries/System.Formats.Cbor/tests/System.Formats.Cbor.Tests.csproj

// Source files and tests for System.Uri
// $ find | grep -i system.uri | sort
// ./src/libraries/System.Private.Uri/src/System/UriBuilder.cs
// ./src/libraries/System.Private.Uri/src/System/UriCreationOptions.cs
// ./src/libraries/System.Private.Uri/src/System/Uri.cs
// ./src/libraries/System.Private.Uri/src/System/UriEnumTypes.cs
// ./src/libraries/System.Private.Uri/src/System/UriExt.cs
// ./src/libraries/System.Private.Uri/src/System/UriFormatException.cs
// ./src/libraries/System.Private.Uri/src/System/UriHelper.cs
// ./src/libraries/System.Private.Uri/src/System/UriHostNameType.cs
// ./src/libraries/System.Private.Uri/src/System/UriParserTemplates.cs
// ./src/libraries/System.Private.Uri/src/System/UriPartial.cs
// ./src/libraries/System.Private.Uri/src/System/UriScheme.cs
// ./src/libraries/System.Private.Uri/src/System/UriSyntax.cs
// ./src/libraries/System.Runtime/tests/System.Runtime.Tests/System/Uri.CreateStringTests.cs
// ./src/libraries/System.Runtime/tests/System.Runtime.Tests/System/Uri.CreateUriTests.cs
// ./src/libraries/System.Runtime/tests/System.Runtime.Tests/System/Uri.MethodsTests.cs

// Take a look at the tests => example based tests

// Relevant standards:
// - Uniform Resource Identifier (URI): Generic Syntax https://datatracker.ietf.org/doc/html/rfc3986
// - URL Living Standard https://url.spec.whatwg.org

// Here we use the RFC as basis for the generator:
// From https://datatracker.ietf.org/doc/html/rfc3986#appendix-A Uniform Resource Identifier (URI): Generic Syntax; Collected ABNF for URI
// https://datatracker.ietf.org/doc/html/rfc5234 Augmented BNF for Syntax Specifications: ABNF

type MyUri = MyUri of string

module UriGen =

    let subdelimsGen: Gen<string> =
        List.ofSeq "!$&'()*+,;=" |> Gen.elements |> Gen.map string

    let hexdigGen: Gen<string> =
        [ '0' .. '9' ] @ [ 'a' .. 'f' ] @ [ 'A' .. 'F' ]
        |> Gen.elements
        |> Gen.map string

    let unreservedGen: Gen<string> =
        [ 'a' .. 'z' ] @ [ 'A' .. 'Z' ] @ [ '0' .. '9' ] @ List.ofSeq "-._~"
        |> Gen.elements
        |> Gen.map string

    let pctencodedGen: Gen<string> =
        Gen.map2 (fun c1 c2 -> "%" + c1 + c2) hexdigGen hexdigGen

    let pcharGen: Gen<string> =
        Gen.oneof [ unreservedGen; pctencodedGen; subdelimsGen; Gen.elements [ ":"; "@" ] ]

    let segmentGen: Gen<string> = pcharGen |> Gen.listOf |> Gen.map (String.concat "")

    let segmentnzGen: Gen<string> =
        pcharGen |> Gen.nonEmptyListOf |> Gen.map (String.concat "")

    let segmentnzncGen: Gen<string> =
        Gen.oneof [ unreservedGen; pctencodedGen; subdelimsGen; gen { return "@" } ]
        |> Gen.nonEmptyListOf
        |> Gen.map (String.concat "")

    let pathemptyGen: Gen<string> = gen { return "" }

    let pathrootlessGen: Gen<string> =
        Gen.map2
            (fun a b -> a + b)
            segmentnzGen
            (segmentGen
             |> Gen.map (fun s -> "/" + s)
             |> Gen.listOf
             |> Gen.map (String.concat ""))

    let pathabsoluteGen: Gen<string> =
        Gen.map
            (fun v -> "/" + string (Option.defaultValue "" v))
            (Gen.optionOf (
                Gen.map2
                    (fun a b -> a + b)
                    segmentnzGen
                    (segmentGen
                     |> Gen.map (fun s -> "/" + s)
                     |> Gen.listOf
                     |> Gen.map (String.concat ""))
            ))

    let pathabemptyGen: Gen<string> =
        Gen.optionOf segmentGen
        |> Gen.map (fun seg -> seg |> Option.map (fun s -> "/" + s) |> Option.defaultValue "")
        |> Gen.listOf
        |> Gen.map (String.concat "")

    let regnameGen: Gen<string> =
        Gen.oneof [ unreservedGen; pctencodedGen; subdelimsGen ]
        |> Gen.listOf
        |> Gen.map (String.concat "")

    let decoctetGen: Gen<string> = Gen.choose (0, 255) |> Gen.map string

    let ipv4addressGen: Gen<string> =
        Gen.listOfLength 4 decoctetGen |> Gen.map (String.concat ".")

    // Note: this is simplified.
    let ipv6addressGen: Gen<string> =
        let h16 = hexdigGen |> Gen.listOfLength 4 |> Gen.map (String.concat "")
        h16 |> Gen.listOfLength 8 |> Gen.map (String.concat ":")

    let ipvfutureGen: Gen<string> =
        Gen.map2
            (fun a b -> "v" + a + "." + b)
            (hexdigGen |> Gen.nonEmptyListOf |> Gen.map (String.concat ""))
            (Gen.oneof [ unreservedGen; subdelimsGen; gen { return ":" } ]
             |> Gen.nonEmptyListOf
             |> Gen.map (String.concat ""))

    let ipliteralGen: Gen<string> =
        Gen.oneof [ ipv6addressGen; ipvfutureGen ] |> Gen.map (fun v -> "[" + v + "]")

    // let portGen =
    //     Gen.elements [ '0' .. '9' ]
    //     |> Gen.map string
    //     |> Gen.listOf
    //     |> Gen.map (String.concat "")
    // Note: Does not generate leading 0s.
    let portGen: Gen<string> = Gen.choose (0, 0xffff) |> Gen.map string

    // let hostGen = Gen.oneof [ ipliteralGen; ipv4addressGen; regnameGen ]
    let hostGen: Gen<string> = Arb.Default.HostName().Generator |> Gen.map string

    let userinfoGen: Gen<string> =
        Gen.oneof [ unreservedGen; pctencodedGen; subdelimsGen; gen { return ":" } ]
        |> Gen.listOf
        |> Gen.map (String.concat "")

    let authorityGen: Gen<string> =
        Gen.map3
            (fun userinfo host port ->
                (userinfo |> Option.map (fun s -> s + "@") |> Option.defaultValue "")
                + host
                + (port |> Option.map (fun s -> ":" + s) |> Option.defaultValue ""))
            (Gen.optionOf userinfoGen)
            hostGen
            (Gen.optionOf portGen)

    let schemeGen: Gen<string> =
        Gen.map2
            (fun h t -> [ h ] @ t |> List.toArray |> System.String)
            ([ 'a' .. 'z' ] @ [ 'A' .. 'Z' ] |> Gen.elements)
            ([ 'a' .. 'z' ] @ [ 'A' .. 'Z' ] @ [ '0' .. '9' ] @ List.ofSeq "+-."
             |> Gen.elements
             |> Gen.listOf)
        |> Gen.filter (fun s -> s.Length > 2)

    let hierpartGen: Gen<string> =
        Gen.oneof
            [ Gen.map2 (fun a b -> "//" + a + b) authorityGen pathabemptyGen
              pathabsoluteGen
              pathrootlessGen
              pathemptyGen ]

    let queryGen: Gen<string> =
        Gen.oneof [ pcharGen; Gen.elements [ "/"; "?" ] ]
        |> Gen.listOf
        |> Gen.map (String.concat "")

    let absoluteUriGen: Gen<MyUri> =
        (schemeGen, hierpartGen, Gen.optionOf queryGen)
        |||> Gen.map3 (fun scheme hierpart query ->
            let uriString =
                scheme
                + ":"
                + hierpart
                + (query |> Option.map (fun s -> "?" + s) |> Option.defaultValue "")

            // System.Uri(uriString, System.UriKind.Absolute))
            MyUri uriString)

    // absoluteUriGen |> Gen.sample 10 10 |> List.map string

    type MyGenerators =
        static member MyUri() =
            { new Arbitrary<MyUri>() with
                override _.Generator = absoluteUriGen
                override _.Shrinker _ = Seq.empty }

    Arb.register<MyGenerators> ()

Arb.generate<MyUri> |> Gen.sample 10 5

let propRoundTrip (uri: MyUri) =
    let (MyUri uri) = uri
    let uri_ = System.Uri(uri, System.UriKind.Absolute).ToString()
    let uri, uri_ = uri.ToLower(), uri_.ToLower()
    printfn "%s" uri
    printfn "%s" uri_
    uri = uri_

Check.Quick propRoundTrip
// Falsifiable, but false positive due to percent encoding.

let propNormalizationIsIdempotent (uria: MyUri) =
    let (MyUri uria) = uria
    let uria = System.Uri(uria, System.UriKind.Absolute)
    let a = uria.ToString()

    let urib = System.Uri(a, System.UriKind.Absolute)
    let b = urib.ToString()

    if a <> b then
        printfn ">%s<" a
        printfn ">%s<" b
    else
        ()

    a = b

Check.One({ Config.Quick with MaxTest = 100000 }, propNormalizationIsIdempotent)

// The following two behaviors could potentially be bugs (could also be that I misunderstood):

// >rcsg8:?Q/+? <
// >rcsg8:?Q/+?<
// Original: MyUri "RcsG8:?Q/+?%20"
Check.One(
    { Config.Quick with
        Replay = Some(Random.StdGen(1697047952, 297581869))
        MaxTest = 100000 },
    propNormalizationIsIdempotent
)

// >xfj:g|//q9?I<
// >xfj:g://q9?I<
// Original: MyUri "Xfj:g%7C//q9?I"
Check.One(
    { Config.Quick with
        Replay = Some(Random.StdGen(564379221, 297581875))
        MaxTest = 100000 },
    propNormalizationIsIdempotent
)

let propNormalizeDotSegments (uri: MyUri) =
    let (MyUri uri) = uri
    let uri = System.Uri(uri, System.UriKind.Absolute)
    not (Array.contains ".." uri.Segments)

Check.Quick propNormalizeDotSegments
Check.One({ Config.Quick with MaxTest = 100000 }, propNormalizeDotSegments) // Passes.

let propNormalizeDotSegments2 (uri: MyUri) =
    let (MyUri uri) = uri
    let path = System.Uri(uri, System.UriKind.Absolute).AbsolutePath

    if path.Contains "/../" then
        printfn "uri: >%s< path >%s<" uri path
    else
        ()

    not (path.Contains "/../")

Check.Quick propNormalizeDotSegments2
Check.One({ Config.Quick with MaxTest = 100000 }, propNormalizeDotSegments2)
// Throws exception (maybe .NET implements some more protocol-specific aspects than the RFC? I don't think it's an issue).

let propNormalizeDotSegments3 (uri: MyUri) =
    let (MyUri uri) = uri
    let path = System.Uri(uri, System.UriKind.Absolute).AbsolutePath

    if path.Contains "/./" then
        printfn "uri: >%s< path >%s<" uri path
    else
        ()

    not (path.Contains "/./")

Check.One({ Config.Quick with MaxTest = 100000 }, propNormalizeDotSegments3) // Falsifiable.

// This could potentially be a bug (could also be that I misunderstood):
// uri: >xmGZ:/.///b?y< path >/.///b<
Check.One(
    { Config.Quick with
        Replay = Some(Random.StdGen(78118126, 297581872))
        MaxTest = 100000 },
    propNormalizeDotSegments3
)
