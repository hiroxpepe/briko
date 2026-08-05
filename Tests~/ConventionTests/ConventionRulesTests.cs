// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable
using NUnit.Framework;
using System.Linq;

namespace Briko.Tests.Convention;

/// <summary>
/// Verifies the rules themselves against mock code. Without this, a green
/// convention run only proves the scan ran, not that it can catch anything.
/// Every rule gets a dirty case (must be caught) and a clean case (must pass).
/// </summary>
[TestFixture]
[Category("Convention")]
public class ConventionRulesTests
{
    static bool caught(string code, string needle) =>
        ConventionRules.find_naming_violations(code, "mock.cs").Any(v => v.Contains(needle));

    static int naming_count(string code) =>
        ConventionRules.find_naming_violations(code, "mock.cs").Count;

    static int order_count(string code) =>
        ConventionRules.find_order_violations(code, "mock.cs").Count;

    // ---- naming: dirty cases must be caught ------------------------------

    [Test]
    public void Catches_PrivateFieldNotSnakeCase()
    {
        Assert.That(caught("class Mock { int badField; }", "must be _snake_case"), Is.True);
    }

    [Test]
    public void Catches_OwnNamespaceUsingBeforeThirdParty()
    {
        var code = "namespace Briko.Editor {\n"
            + "    using System;\n"
            + "    using Briko.Editor.Internal;\n"
            + "    using UnityEngine;\n"
            + "    class Mock {}\n"
            + "}";
        var found = ConventionRules.find_using_order_violations(code, "mock.cs");
        Assert.That(found.Any(v => v.Contains("out of group order")), Is.True);
    }

    [Test]
    public void Passes_UsingsInSystemThenThirdPartyThenOwnOrder()
    {
        var code = "namespace Briko.Editor {\n"
            + "    using System;\n"
            + "    using UnityEngine;\n"
            + "    using Briko.Editor.Internal;\n"
            + "    class Mock {}\n"
            + "}";
        var found = ConventionRules.find_using_order_violations(code, "mock.cs");
        Assert.That(found.Any(v => v.Contains("out of group order")), Is.False);
    }

    [Test]
    public void Catches_OpeningBraceOnItsOwnLine()
    {
        var code = "class Mock\n{\n    void run() {}\n}";
        var found = ConventionRules.find_brace_violations(code, "mock.cs");
        Assert.That(found.Any(v => v.Contains("opening brace must join the line above")), Is.True);
    }

    [Test]
    public void Passes_OpeningBraceOnTheSameLine()
    {
        var code = "class Mock {\n    void run() {}\n}";
        var found = ConventionRules.find_brace_violations(code, "mock.cs");
        Assert.That(found.Any(v => v.Contains("opening brace must join the line above")), Is.False);
    }

    [Test]
    public void Catches_ExplicitPrivateKeyword()
    {
        Assert.That(caught("class Mock { private void run() {} }", "must omit the redundant 'private' keyword"), Is.True);
    }

    [Test]
    public void Passes_ImplicitPrivateWithNoKeyword()
    {
        Assert.That(caught("class Mock { void run() {} }", "must omit the redundant 'private' keyword"), Is.False);
    }

    [Test]
    public void Catches_ConstNotUpperSnake()
    {
        Assert.That(caught("class Mock { const int maxSize = 1; }", "must be UPPER_SNAKE"), Is.True);
    }

    [Test]
    public void Catches_LocalNotSnakeCase()
    {
        Assert.That(caught("class Mock { void run() { var itemCount = 1; } }", "local 'itemCount'"), Is.True);
    }

    [Test]
    public void Catches_ForEachVarNotSnakeCase()
    {
        Assert.That(caught("class Mock { void run() { foreach (var eachItem in x) {} } }", "foreach var 'eachItem'"), Is.True);
    }

    [Test]
    public void Catches_ParameterNotSnakeCase()
    {
        Assert.That(caught("class Mock { void run(int tabId) {} }", "parameter 'tabId'"), Is.True);
    }

    [Test]
    public void Catches_PublicMethodNotPascalCase()
    {
        Assert.That(caught("class Mock { public void doWork() {} }", "must be PascalCase"), Is.True);
    }

    [Test]
    public void Catches_PrivateMethodNotCamelCase()
    {
        Assert.That(caught("class Mock { private void DoWork() {} }", "must be camelCase"), Is.True);
    }

    [Test]
    public void Catches_EnumMemberNotPascalCase()
    {
        Assert.That(caught("enum M { first_value }", "must be PascalCase"), Is.True);
    }

    [Test]
    public void Catches_AbbreviationNotExpanded()
    {
        Assert.That(caught("class Mock { public void SendMsgNow() {} }", "unknown word part 'Msg'"), Is.True);
    }

    [Test]
    public void Catches_AcronymNotUpperCased()
    {
        Assert.That(caught("class Mock { public void ReadDomTree() {} }", "letter word 'Dom', use 'DOM'"), Is.True);
    }

    // ---- naming: clean cases must pass -----------------------------------

    [Test]
    public void Passes_CleanNaming()
    {
        var code = @"
enum TabState { Idle, Stopped }

class Watcher
{
    const int MAX_TABS = 8;
    static readonly string DEFAULT_URL = ""x"";
    int _tab_count;

    public void Start(int tab_index)
    {
        var next_state = TabState.Idle;
        foreach (var each_tab in tabs) { }
    }

    void reset() { }
}";
        Assert.That(naming_count(code), Is.Zero,
            string.Join("\n  ", ConventionRules.find_naming_violations(code, "mock.cs")));
    }

    [Test]
    public void Skips_OverrideMemberParameters()
    {
        // An override signature comes from outside, so its parameter names are exempt.
        Assert.That(naming_count("class Mock { public override void OnCreate(int savedState) {} }"), Is.Zero);
    }

    [Test]
    public void Allows_AcronymAlreadyUpperCased()
    {
        Assert.That(naming_count("class Mock { public void ReadDOMTree() {} }"), Is.Zero);
    }

    [Test]
    public void Allows_WordThatMerelyContainsAcronymLetters()
    {
        // 'Region' contains 'io' but not as a hump, so it must not be flagged.
        Assert.That(naming_count("class Mock { public void FindRegion() {} }"), Is.Zero);
    }


    [Test]
    public void Ignores_ExternalApiNamesWhenSpelling()
    {
        // Calling an SDK member named LoadUrl is not ours to rename.
        Assert.That(naming_count("class Mock { void run() { view.LoadUrl(site); } }"), Is.Zero);
    }

    [Test]
    public void Ignores_ExternalPropertyNamesWhenSpelling()
    {
        Assert.That(naming_count("class Mock { void run() { settings.DomStorageEnabled = true; } }"), Is.Zero);
    }

    [Test]
    public void Ignores_ExternDeclarations()
    {
        // The name of an imported function is fixed by the platform. It cannot be
        // renamed, so holding it to our casing would only force it to be silenced.
        var code = "class Mock { static extern int DwmSetWindowAttribute(int window); }";
        Assert.That(naming_count(code), Is.Zero);
    }

    [Test]
    public void Catches_AbbreviationInDeclaredTypeName()
    {
        Assert.That(caught("class MsgBox { }", "unknown word part 'Msg'"), Is.True);
    }

    // ---- order -----------------------------------------------------------

    [Test]
    public void Catches_MethodBeforeField()
    {
        var code = "class Mock { public void Run() {} int _count; }";
        Assert.That(order_count(code), Is.GreaterThan(0));
    }

    [Test]
    public void Catches_PublicMethodAfterPrivateMethod()
    {
        var code = "class Mock { void helper() {} public void Run() {} }";
        Assert.That(order_count(code), Is.GreaterThan(0));
    }

    [Test]
    public void Catches_InstanceFieldBeforeConst()
    {
        var code = "class Mock { int _count; const int MAX = 1; }";
        Assert.That(order_count(code), Is.GreaterThan(0));
    }

    [Test]
    public void Passes_CleanOrder()
    {
        var code = @"
class Watcher
{
    const int MAX_TABS = 8;
    static int _shared_count;
    int _tab_count;

    public Watcher() { }

    public int TabCount { get; }

    public void Start() { }

    void reset() { }
}";
        Assert.That(order_count(code), Is.Zero,
            string.Join("\n  ", ConventionRules.find_order_violations(code, "mock.cs")));
    }

    [Test]
    public void Ignores_InterfaceDeclarations()
    {
        Assert.That(order_count("interface I { void Run(); int Count { get; } }"), Is.Zero);
    }

    // ---- letter words: snake keeps lower, Pascal wants all caps ----------

    [Test]
    public void Allows_LowerCaseLetterWordInSnakeName()
    {
        // A snake_case name is all lower case, so 'id' in 'item_id' is fine.
        Assert.That(naming_count("class Mock { void run(int item_id) {} }"), Is.Zero);
    }

    [Test]
    public void Catches_LetterWordOnlyCapitalized()
    {
        // 'Id' in a PascalCase name must be the all-caps print form 'ID'.
        Assert.That(caught("class Mock { public int NodeId() => 0; }",
            "'NodeId' has the letter word 'Id', use 'ID'"), Is.True);
    }

    [Test]
    public void Allows_LetterWordAllCaps()
    {
        // 'ID' is already the print form, so it passes.
        Assert.That(naming_count("class Mock { public int NodeID() => 0; }"), Is.Zero);
    }

    [Test]
    public void Allows_NormalWordStartingUpper()
    {
        // 'Node' is a plain word, not a letter word, so PascalCase is fine.
        Assert.That(naming_count("class Mock { public int NodeName() => 0; }"), Is.Zero);
    }

    [Test]
    public void Allows_PluralLetterWord()
    {
        // URLs is the plural of the letter word URL: it splits as URL + s,
        // not UR + Ls, so it passes as an all-caps letter word.
        Assert.That(naming_count("class Mock { public int NodeURLs() => 0; }"), Is.Zero);
    }

    // ---- single letters: only the habitual ones pass ---------------------

    [Test]
    public void Allows_HabitualSingleLetter()
    {
        // 'i' is a long-standing loop name, so it passes.
        Assert.That(naming_count("class Mock { void run() { for (var i = 0; i < 3; i++) {} } }"), Is.Zero);
    }

    [Test]
    public void Catches_UnlistedSingleLetter()
    {
        // 'g' is not in the habitual set, so a one-letter 'g' is too short.
        Assert.That(caught("class Mock { void run() { for (var g = 0; g < 3; g++) {} } }",
            "the one-letter name 'g'"), Is.True);
    }
}
