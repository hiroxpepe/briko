// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Briko.Tests.Convention;

/// <summary>
/// The rules themselves. Pure: takes a source string, returns violation strings.
/// No filesystem, no paths, no compilation. That is what makes them testable with
/// mock code snippets.
///
/// Naming rules
///   1. private / mutable-static fields  -> _snake_case
///   2. const / static-readonly fields   -> UPPER_SNAKE
///   3. locals, foreach vars, parameters -> snake_case
///   4. exposed methods / properties     -> PascalCase
///   5. private methods / properties     -> camelCase
///   6. enum members                     -> PascalCase
///   7. spelling: expand abbreviations, upper-case true acronyms
///
/// Order rule
///   StyleCop element kind, then accessibility, then static-before-instance.
///
/// PORTING: only EXPAND / UPPER are repo specific.
/// </summary>
static class ConventionRules
{
    static readonly Regex SNAKE = new(@"^[a-z][a-z0-9_]*$", RegexOptions.Compiled);
    static readonly Regex SNAKE_FIELD = new(@"^_[a-z][a-z0-9_]*$", RegexOptions.Compiled);
    static readonly Regex UPPER_SNAKE = new(@"^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled);
    static readonly Regex PASCAL = new(@"^[A-Z][A-Za-z0-9]*$", RegexOptions.Compiled);
    static readonly Regex CAMEL = new(@"^[a-z][A-Za-z0-9]*$", RegexOptions.Compiled);

    // A name is spelled from known words. The word lists hold the words this
    // project accepts. PORTING: only the word lists are repo specific.
    //   plain_words   — Basic English plus the plain code words of this repo
    //   project_words — the made-up names of this repo (webio, crown, ...)
    //   unit_marks    — unit marks kept in print form (hz, db, ms, ...)
    static readonly HashSet<string> BASIC_WORDS = load_words("basic_words.md");
    static readonly HashSet<string> LANG_WORDS = load_words("lang_words.md");
    static readonly HashSet<string> PLAIN_WORDS = load_words("plain_words.md");
    static readonly HashSet<string> DRAFT_WORDS = load_words("draft_words.md");
    static readonly HashSet<string> PROJECT_WORDS = load_words("project_words.md");
    static readonly HashSet<string> UNIT_WORDS = load_words("unit_words.md");
    static readonly HashSet<string> LETTER_WORDS = load_words("letter_words.md");
    static readonly HashSet<string> SINGLE_WORDS = load_words("single_words.md");
    // Methods a framework calls by name (Unity messages like OnEnable). Their
    // names are fixed by the framework, so the casing check skips them. The file
    // is optional: a plain dotnet project has none, so the set is empty and the
    // check behaves as before.
    static readonly HashSet<string> UNITY_METHODS = load_words("unity_methods.md");
    static readonly HashSet<string> TECH_TERMS = load_tech_terms();

    // The tech-terms list is the same one the documents use. Each entry is a
    // line "**term** — sense", so the term is the text in the first bold span.
    static HashSet<string> load_tech_terms()
    {
        var here = Path.GetDirectoryName(typeof(ConventionRules).Assembly.Location) ?? ".";
        for (var dir = here; dir != null; dir = Path.GetDirectoryName(dir)) {
            var path = Path.Combine(dir, "docs", "standard", "tech_terms.md");
            if (File.Exists(path)) {
                var set = new HashSet<string>();
                foreach (var line in File.ReadAllLines(path)) {
                    var m = Regex.Match(line, @"^\*\*([A-Za-z][A-Za-z0-9 ]*)\*\*");
                    if (m.Success)
                        foreach (var w in m.Groups[1].Value.Split(' '))
                            if (w.Length > 0) set.Add(w.ToLowerInvariant());
                }
                return set;
            }
        }
        return new HashSet<string>();
    }

    static HashSet<string> load_words(string file_name)
    {
        var here = Path.GetDirectoryName(typeof(ConventionRules).Assembly.Location) ?? ".";
        // Walk up to the test project folder, where the vocabulary folder sits.
        for (var dir = here; dir != null; dir = Path.GetDirectoryName(dir)) {
            var path = Path.Combine(dir, "vocabulary", file_name);
            if (File.Exists(path))
                return new HashSet<string>(
                    File.ReadAllLines(path)
                        .Where(line => line.StartsWith("+ "))
                        .SelectMany(line => line.Substring(2).Trim().ToLowerInvariant()
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        .Where(w => w.Length > 0));
        }
        return new HashSet<string>();
    }

    // Split a name into its word parts, keeping the original case of each part:
    // "buildTabCount" -> build, Tab, Count; "NodeID" -> Node, ID;
    // "CrownGoURLs" -> Crown, Go, URL, s; "read_answer" -> read, answer.
    static IEnumerable<string> word_parts(string identifier)
    {
        var trimmed = identifier.Trim('_');
        // A run of caps that ends in a lone lower 's' is a plural letter word
        // (URLs -> URL + s), so the caps run stops before that 's'.
        // Priority order: a floor label (2F, B1F) or a 6-digit hex color or a
        // digit+unit suffix (100K, 1KB) is claimed whole before the generic
        // digit-run-then-caps-run catch-all gets a chance to split it
        // differently — that catch-all still fires for anything else digit-led
        // (2GARBAGE, 3JUNK), keeping those unregistered and rejected.
        foreach (Match m in Regex.Matches(trimmed,
                @"B?[0-9]+F(?![a-zA-Z0-9])" +
                @"|(?=[0-9A-F]{6}(?![A-Za-z0-9]))(?=[0-9A-F]*[0-9])[0-9A-F]{6}" +
                @"|[0-9]+(?:KB|MB|GB|TB|KHZ|MHZ|GHZ|HZ|MS|K)(?![A-Za-z0-9])" +
                @"|[0-9]+[A-Z]+(?![a-z])|[A-Z]+(?=s(?![a-z]))|[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z0-9]+"))
            yield return m.Value;
    }

    // A 6-character hex color literal (E2246B, FFFFFF): all hex digits, at
    // least one actual digit so a plain word like "ABCDEF" still goes through
    // the ordinary all-caps acronym path instead of this one.
    static bool is_hex_color(string part) =>
        part.Length == 6 && part.All(c => "0123456789ABCDEF".Contains(c)) && part.Any(char.IsDigit);

    // A digit run followed by a known size/frequency unit suffix (100K, 1KB,
    // 60Hz already splits into 60+Hz on its own): the unit is fixed by
    // convention, not a project word, so no registration is needed.
    static readonly string[] UNIT_SUFFIXES = { "KB", "MB", "GB", "TB", "KHZ", "MHZ", "GHZ", "HZ", "MS", "K" };
    static bool is_digit_with_unit_suffix(string part) {
        if (!char.IsDigit(part[0])) return false;
        foreach (var suffix in UNIT_SUFFIXES) {
            if (part.EndsWith(suffix, StringComparison.Ordinal)) {
                var digits = part.Substring(0, part.Length - suffix.Length);
                if (digits.Length > 0 && digits.All(char.IsDigit)) return true;
            }
        }
        return false;
    }

    static bool known_word(string part)
    {
        var lower = part.ToLowerInvariant();
        return BASIC_WORDS.Contains(lower) || LANG_WORDS.Contains(lower) || PLAIN_WORDS.Contains(lower) || DRAFT_WORDS.Contains(lower) || PROJECT_WORDS.Contains(lower)
            || UNIT_WORDS.Contains(lower) || TECH_TERMS.Contains(lower)
            || part.All(char.IsDigit)
            || is_hex_color(part) || is_digit_with_unit_suffix(part);
    }

    // An all-caps letter word (ID, API, JSON): two or more letters, all upper
    // case, in the raw name. This is the print form of a letter word, so it is
    // accepted as is. A part like "Id" is not all caps, so it is not accepted
    // here — it must be spelled "ID" or be a known plain word.
    static bool is_all_caps_letter_word(string part) =>
        part.Length >= 2 && part.All(char.IsUpper);

    // ---- naming ----------------------------------------------------------

    internal static List<string> find_naming_violations(string code, string label)
    {
        var found = new List<string>();
        var root = CSharpSyntaxTree.ParseText(code).GetRoot();

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>()) {
            bool is_const = has(field.Modifiers, "const");
            bool is_static_readonly = has(field.Modifiers, "static") && has(field.Modifiers, "readonly");
            foreach (var variable in field.Declaration.Variables) {
                var id = variable.Identifier.ValueText;
                if (is_const || is_static_readonly) {
                    if (!UPPER_SNAKE.IsMatch(id))
                        found.Add($"{label}:{line(variable)}: const '{id}' must be UPPER_SNAKE");
                } else if (exposed(field.Modifiers)) {
                    // An exposed mutable field on a [Serializable] type is a
                    // JSON-mapping field, and one on a [StructLayout] type is a
                    // mirror of an outside (native) structure: snake_case is its
                    // external form. Anywhere else an exposed field is PascalCase.
                    if (in_serializable_type(variable) || in_interop_struct(variable)) {
                        if (!PASCAL.IsMatch(id) && !SNAKE.IsMatch(id))
                            found.Add($"{label}:{line(variable)}: outside-shape field '{id}' must be snake_case or PascalCase");
                    } else if (!PASCAL.IsMatch(id)) {
                        found.Add($"{label}:{line(variable)}: field '{id}' must be PascalCase");
                    }
                } else {
                    if (!SNAKE_FIELD.IsMatch(id))
                        found.Add($"{label}:{line(variable)}: field '{id}' must be _snake_case");
                }
            }
        }

        foreach (var declarator in root.DescendantNodes().OfType<VariableDeclaratorSyntax>()) {
            if (declarator.Parent?.Parent is FieldDeclarationSyntax) continue;
            // Event field declarations are members, not locals; they are checked
            // by the dedicated event loop below with the exposed/PascalCase rule.
            if (declarator.Parent?.Parent is EventFieldDeclarationSyntax) continue;
            var id = declarator.Identifier.ValueText;
            if (!SNAKE.IsMatch(id))
                found.Add($"{label}:{line(declarator)}: local '{id}' must be snake_case");
        }

        foreach (var ev in root.DescendantNodes().OfType<EventFieldDeclarationSyntax>()) {
            if (has(ev.Modifiers, "override")) continue;
            foreach (var variable in ev.Declaration.Variables)
                check_casing(found, label, variable, variable.Identifier.ValueText, ev.Modifiers, "event");
        }

        foreach (var ev in root.DescendantNodes().OfType<EventDeclarationSyntax>()) {
            if (has(ev.Modifiers, "override")) continue;
            if (ev.ExplicitInterfaceSpecifier != null) continue;
            check_casing(found, label, ev, ev.Identifier.ValueText, ev.Modifiers, "event");
        }

        foreach (var each in root.DescendantNodes().OfType<ForEachStatementSyntax>()) {
            var id = each.Identifier.ValueText;
            if (!SNAKE.IsMatch(id))
                found.Add($"{label}:{line(each)}: foreach var '{id}' must be snake_case");
        }

        foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>()) {
            var id = parameter.Identifier.ValueText;
            if (id.Length == 0) continue;
            if (in_overriding_member(parameter)) continue;
            if (in_extern_member(parameter)) continue;
            if (!SNAKE.IsMatch(id))
                found.Add($"{label}:{line(parameter)}: parameter '{id}' must be snake_case");
        }

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()) {
            if (has(method.Modifiers, "override")) continue;
            if (has(method.Modifiers, "extern")) continue;
            if (method.ExplicitInterfaceSpecifier != null) continue;
            if (method.Parent is InterfaceDeclarationSyntax) continue;
            if (UNITY_METHODS.Contains(method.Identifier.ValueText.ToLowerInvariant())) continue;
            check_casing(found, label, method, method.Identifier.ValueText, method.Modifiers, "method");
        }

        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>()) {
            if (has(property.Modifiers, "override")) continue;
            if (property.ExplicitInterfaceSpecifier != null) continue;
            if (property.Parent is InterfaceDeclarationSyntax) continue;
            // A public property on a [Serializable] type is a JSON-mapping
            // property: its name is the external JSON key, so snake_case is
            // correct and PascalCase is not required. See tech_terms JSON entry.
            if (exposed(property.Modifiers) && in_serializable_type(property)) {
                var pid = property.Identifier.ValueText;
                if (!PASCAL.IsMatch(pid) && !SNAKE.IsMatch(pid))
                    found.Add($"{label}:{line(property)}: json property '{pid}' must be snake_case or PascalCase");
                continue;
            }
            check_casing(found, label, property, property.Identifier.ValueText, property.Modifiers, "property");
        }

        foreach (var member in root.DescendantNodes().OfType<EnumMemberDeclarationSyntax>()) {
            var id = member.Identifier.ValueText;
            if (!PASCAL.IsMatch(id))
                found.Add($"{label}:{line(member)}: enum member '{id}' must be PascalCase");
        }

        // Type names (class, struct, interface, enum, record) are always
        // PascalCase. The print rule holds here too: a letter word in a type
        // name is all caps (JSON, not Json), enforced by the spelling pass below.
        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()) {
            var id = type.Identifier.ValueText;
            if (!PASCAL.IsMatch(id))
                found.Add($"{label}:{line(type)}: type '{id}' must be PascalCase");
        }

        // A member with no access modifier is already private by C# default,
        // so writing the keyword out is redundant noise. Say it once here
        // instead of on every field, method, and property.
        foreach (var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>()) {
            if (has(modifiers_of(member), "private"))
                found.Add($"{label}:{line(member)}: '{name_of(member)}' must omit the redundant 'private' keyword");
        }

        // Namespace names are PascalCase in every dotted segment (Animo.Core,
        // not animo.core). The spelling pass below also holds for each segment.
        foreach (var ns in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()) {
            foreach (var seg in ns.Name.ToString().Split('.')) {
                if (!PASCAL.IsMatch(seg))
                    found.Add($"{label}:{line(ns)}: namespace segment '{seg}' must be PascalCase");
            }
        }

        // Spelling applies only to names WE declare. Names that come from outside
        // (platform and SDK members) are not ours to rename, so call sites and
        // member accesses are not scanned.
        //
        // A name is spelled from known words: each word part is a plain word, a
        // project word, or a unit mark; or it is an all-caps letter word. A part
        // that is none of these is a short form or a hard word, and is flagged.
        foreach (var (id, at) in declared_names(root)) {
            var parts = word_parts(id).ToList();
            for (var pi = 0; pi < parts.Count; pi++) {
                var part = parts[pi];
                // A lone 's' right after a letter word is the plural marker
                // (URLs -> URL, s), so it is not a one-letter name.
                if (part == "s" && pi > 0 && parts[pi - 1].All(char.IsUpper)) continue;
                if (part.Length == 1) {
                    if (char.IsDigit(part[0])) continue;
                    if (!SINGLE_WORDS.Contains(part.ToLowerInvariant()))
                        found.Add($"{label}:{at}: '{id}' has the one-letter name '{part}', use a full word");
                    continue;
                }
                // A letter word must be in its all-caps print form. 'Id' (only
                // capitalized) must be 'ID'. A lower-case 'id' inside a
                // snake_case name is fine, so only a capitalized-not-all-caps
                // part is held here.
                var lower = part.ToLowerInvariant();
                if (LETTER_WORDS.Contains(lower) && !part.All(char.IsUpper) && char.IsUpper(part[0])) {
                    found.Add($"{label}:{at}: '{id}' has the letter word '{part}', use '{lower.ToUpperInvariant()}'");
                    continue;
                }
                if (known_word(part)) continue;
                if (is_all_caps_letter_word(part)) continue;
                found.Add($"{label}:{at}: '{id}' has the unknown word part '{part}', use a full plain word");
            }
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    // Identifiers introduced by this file: types, members, locals, parameters.
    // Overrides and explicit interface implementations are excluded because their
    // names are fixed by the external type they come from.
    static IEnumerable<(string id, int at)> declared_names(SyntaxNode root)
    {
        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            yield return (type.Identifier.ValueText, line(type));

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()) {
            if (has(method.Modifiers, "override") || has(method.Modifiers, "extern")) continue;
            if (method.ExplicitInterfaceSpecifier != null) continue;
            if (UNITY_METHODS.Contains(method.Identifier.ValueText.ToLowerInvariant())) continue;
            yield return (method.Identifier.ValueText, line(method));
        }

        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>()) {
            if (has(property.Modifiers, "override") || property.ExplicitInterfaceSpecifier != null) continue;
            yield return (property.Identifier.ValueText, line(property));
        }

        foreach (var declarator in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            yield return (declarator.Identifier.ValueText, line(declarator));

        foreach (var ns in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
            foreach (var seg in ns.Name.ToString().Split('.'))
                yield return (seg, line(ns));

        foreach (var each in root.DescendantNodes().OfType<ForEachStatementSyntax>())
            yield return (each.Identifier.ValueText, line(each));

        foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>()) {
            if (parameter.Identifier.ValueText.Length == 0) continue;
            if (in_overriding_member(parameter)) continue;
            if (in_extern_member(parameter)) continue;
            yield return (parameter.Identifier.ValueText, line(parameter));
        }

        foreach (var member in root.DescendantNodes().OfType<EnumMemberDeclarationSyntax>())
            yield return (member.Identifier.ValueText, line(member));
    }

    static void check_casing(List<string> found, string label, SyntaxNode node,
        string id, SyntaxTokenList modifiers, string kind)
    {
        bool want_pascal = exposed(modifiers);
        bool ok = want_pascal ? PASCAL.IsMatch(id) : CAMEL.IsMatch(id);
        if (!ok)
            found.Add($"{label}:{line(node)}: {kind} '{id}' must be {(want_pascal ? "PascalCase" : "camelCase")}");
    }

    // ---- file name ------------------------------------------------------

    // The file name (without .cs) must follow the same print rule as a type
    // name: no short forms, letter words in all caps. A file holding type JSON
    // is JSON.cs, not Json.cs.
    internal static List<string> find_filename_violations(string file_name)
    {
        var found = new List<string>();
        var stem = file_name.EndsWith(".cs") ? file_name.Substring(0, file_name.Length - 3) : file_name;
        foreach (var part in word_parts(stem)) {
            if (part.Length == 1) {
                if (!SINGLE_WORDS.Contains(part.ToLowerInvariant()))
                    found.Add($"{file_name}: file name has the one-letter name '{part}', use a full word");
                continue;
            }
            if (known_word(part)) continue;
            if (is_all_caps_letter_word(part)) continue;
            found.Add($"{file_name}: file name has the unknown word part '{part}', use a full plain word");
        }
        return found;
    }

    // ---- order -----------------------------------------------------------

    internal static List<string> find_order_violations(string code, string label)
    {
        var found = new List<string>();
        var tree = CSharpSyntaxTree.ParseText(code);
        var unit = tree.GetCompilationUnitRoot();

        foreach (var type in unit.DescendantNodes().OfType<TypeDeclarationSyntax>()) {
            if (type is InterfaceDeclarationSyntax) continue;
            var members = type.Members;
            if (members.Count < 2) continue;
            (int, int, int, int) high = (-1, -1, -1, -1);
            foreach (var member in members) {
                var key = key_of(member);
                if (key.CompareTo(high) < 0) {
                    var at = tree.GetLineSpan(member.Span).StartLinePosition.Line + 1;
                    found.Add($"{label}:{at}: '{type.Identifier.Text}.{name_of(member)}' is out of StyleCop order");
                }
                if (key.CompareTo(high) > 0) high = key;
            }
        }
        found.Sort(StringComparer.Ordinal);
        return found;
    }

    // A type, a method body, or a control-flow block must open its brace on
    // the same line as the line before it — `void run() {`, not `void run()`
    // then `{` alone on the next line.
    internal static List<string> find_brace_violations(string code, string label)
    {
        var found = new List<string>();
        var tree = CSharpSyntaxTree.ParseText(code);
        var unit = tree.GetCompilationUnitRoot();

        void check(SyntaxToken open_brace)
        {
            var prev = open_brace.GetPreviousToken();
            if (prev.Kind() == SyntaxKind.None) return;
            var brace_line = tree.GetLineSpan(open_brace.Span).StartLinePosition.Line;
            var prev_line = tree.GetLineSpan(prev.Span).StartLinePosition.Line;
            if (brace_line != prev_line)
                found.Add($"{label}:{brace_line + 1}: opening brace must join the line above, not stand alone");
        }

        foreach (var block in unit.DescendantNodes().OfType<BlockSyntax>())
            check(block.OpenBraceToken);

        foreach (var type in unit.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            check(type.OpenBraceToken);

        foreach (var ns in unit.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
            check(ns.OpenBraceToken);

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    // A file's using directives fall into three groups, in this order:
    // System, then Unity or any other third-party OSS library, then this
    // project's own namespace. Within that, the file need not be
    // alphabetical — only the group order is enforced.
    internal static List<string> find_using_order_violations(string code, string label)
    {
        var found = new List<string>();
        var tree = CSharpSyntaxTree.ParseText(code);
        var unit = tree.GetCompilationUnitRoot();

        var own_ns = unit.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var own_root = own_ns?.Name.ToString().Split('.')[0];

        int group_of(string name)
        {
            var root = name.Split('.')[0];
            if (root == "System") return 0;
            if (own_root != null && root == own_root) return 2;
            return 1;
        }

        int high = -1;
        foreach (var u in unit.DescendantNodes().OfType<UsingDirectiveSyntax>()) {
            if (u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) || u.Alias != null) continue;
            var name = u.Name?.ToString() ?? "";
            var group = group_of(name);
            if (group < high) {
                var at = tree.GetLineSpan(u.Span).StartLinePosition.Line + 1;
                found.Add($"{label}:{at}: using '{name}' is out of group order (system, then third-party, then own code)");
            }
            if (group > high) high = group;
        }
        found.Sort(StringComparer.Ordinal);
        return found;
    }

    // Every source file opens with the same five lines: the copyright line,
    // the license line, a blank line, `#nullable enable`, and another blank
    // line — before anything else, including using directives.
    internal static List<string> find_header_violations(string code, string label)
    {
        var found = new List<string>();
        var lines = code.Replace("\r\n", "\n").Split('\n');
        string at(int i) => i < lines.Length ? lines[i] : "";

        const string copyright = "// Copyright (c) STUDIO MeowToon. All rights reserved.";
        const string license = "// Licensed under the MIT License. See LICENSE in the project root for license information.";

        if (at(0) != copyright)
            found.Add($"{label}:1: header line 1 must be the copyright notice");
        if (at(1) != license)
            found.Add($"{label}:2: header line 2 must be the license notice");
        if (at(2) != "")
            found.Add($"{label}:3: header line 3 must be blank");
        if (at(3) != "#nullable enable")
            found.Add($"{label}:4: header line 4 must be '#nullable enable'");
        if (at(4) != "")
            found.Add($"{label}:5: header line 5 must be blank");

        return found;
    }

    static (int kind, int sub, int acc, int stat) key_of(MemberDeclarationSyntax member)
    {
        int kind = kind_rank(member);
        int sub = member is FieldDeclarationSyntax f ? field_sub(f) : 0;
        var modifiers = modifiers_of(member);
        int stat = has(modifiers, "static") ? 0 : 1;
        int acc = accessibility_rank(modifiers);
        return (kind, sub, acc, stat);
    }

    static int kind_rank(MemberDeclarationSyntax member) => member switch {
        FieldDeclarationSyntax => 0,
        ConstructorDeclarationSyntax => 2,
        DestructorDeclarationSyntax => 3,
        DelegateDeclarationSyntax => 4,
        EventDeclarationSyntax => 5,
        EventFieldDeclarationSyntax => 5,
        EnumDeclarationSyntax => 6,
        InterfaceDeclarationSyntax => 7,
        PropertyDeclarationSyntax => 8,
        IndexerDeclarationSyntax => 9,
        MethodDeclarationSyntax => 10,
        OperatorDeclarationSyntax => 10,
        ConversionOperatorDeclarationSyntax => 10,
        StructDeclarationSyntax => 11,
        ClassDeclarationSyntax => 12,
        RecordDeclarationSyntax => 12,
        _ => 10
    };

    static int field_sub(FieldDeclarationSyntax field)
    {
        if (has(field.Modifiers, "const")) return 0;
        if (has(field.Modifiers, "static")) return 1;
        return 2;
    }

    static int accessibility_rank(SyntaxTokenList modifiers)
    {
        bool is_public = has(modifiers, "public");
        bool is_internal = has(modifiers, "internal");
        bool is_protected = has(modifiers, "protected");
        bool is_private = has(modifiers, "private");
        if (is_public) return 0;
        if (is_protected && is_internal) return 1;
        if (is_internal) return 2;
        if (is_protected && is_private) return 3;
        if (is_protected) return 4;
        return 5;
    }

    static SyntaxTokenList modifiers_of(MemberDeclarationSyntax member) => member switch {
        BaseFieldDeclarationSyntax f => f.Modifiers,
        BaseMethodDeclarationSyntax m => m.Modifiers,
        BasePropertyDeclarationSyntax p => p.Modifiers,
        BaseTypeDeclarationSyntax t => t.Modifiers,
        DelegateDeclarationSyntax d => d.Modifiers,
        _ => default
    };

    static string name_of(MemberDeclarationSyntax member) => member switch {
        MethodDeclarationSyntax m => m.Identifier.Text + "()",
        PropertyDeclarationSyntax p => p.Identifier.Text,
        FieldDeclarationSyntax f => string.Join(",", f.Declaration.Variables.Select(v => v.Identifier.Text)),
        ConstructorDeclarationSyntax => "<ctor>",
        _ => member.Kind().ToString()
    };

    // ---- shared ----------------------------------------------------------

    static bool has(SyntaxTokenList modifiers, string text) => modifiers.Any(m => m.Text == text);

    static bool exposed(SyntaxTokenList modifiers) =>
        has(modifiers, "public") || has(modifiers, "internal") || has(modifiers, "protected");

    // True when the node sits inside a type marked [Serializable]. Such a type
    // is a JSON-mapping DTO, so its public property names are external JSON keys
    // and are allowed to stay snake_case.
    static bool in_serializable_type(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent) {
            if (current is TypeDeclarationSyntax type) {
                foreach (var list in type.AttributeLists)
                    foreach (var attr in list.Attributes) {
                        var name = attr.Name.ToString();
                        if (name == "Serializable" || name == "System.Serializable")
                            return true;
                    }
            }
        }
        return false;
    }

    // True when the node sits inside a type marked [StructLayout]. Such a type
    // is a mirror of an outside (Win32 / native) data structure, so its field
    // names carry that structure's shape and are allowed to stay snake_case,
    // the same way a JSON DTO's keys are.
    static bool in_interop_struct(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent) {
            if (current is TypeDeclarationSyntax type) {
                foreach (var list in type.AttributeLists)
                    foreach (var attr in list.Attributes) {
                        var name = attr.Name.ToString();
                        if (name == "StructLayout" || name == "System.Runtime.InteropServices.StructLayout")
                            return true;
                    }
            }
        }
        return false;
    }

    static bool in_overriding_member(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent) {
            if (current is MethodDeclarationSyntax m)
                return has(m.Modifiers, "override") || m.ExplicitInterfaceSpecifier != null;
            if (current is BasePropertyDeclarationSyntax p)
                return has(p.Modifiers, "override");
        }
        return false;
    }

    static bool in_extern_member(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent) {
            if (current is MethodDeclarationSyntax m)
                return has(m.Modifiers, "extern");
        }
        return false;
    }

    // Matches only as a camelCase hump, so 'Io' hits ReadIoPort but not Region.
    static bool is_hump(string identifier, string token)
    {
        for (int i = 0; i + token.Length <= identifier.Length; i++) {
            if (string.CompareOrdinal(identifier, i, token, 0, token.Length) != 0) continue;
            bool left_ok = i == 0 || char.IsLower(identifier[i - 1]) || char.IsDigit(identifier[i - 1]) || identifier[i - 1] == '_';
            int after = i + token.Length;
            bool right_ok = after == identifier.Length || char.IsUpper(identifier[after]) || char.IsDigit(identifier[after]) || identifier[after] == '_';
            if (left_ok && right_ok) return true;
        }
        return false;
    }

    static int line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    static int line_of_token(SyntaxToken token) => token.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}
