# Naming Exceptions

The standard here is set once, in full, in `coding_standard.md`, and the
same for every project (webio, animo, briko, germio, and the like) — a
public, internal, or protected member is PascalCase, because all three
face a reader who is not the author: internal faces every other file in
the same assembly, and protected faces every subclass, which may live in
a project this repository never sees. Neither is truly private.

This file exists for the rare case where a project has already, on
purpose, given a specific member a different shape, and changing it now
would cost more than it is worth. An entry here is not a second standard;
it is a named, reviewed exception to the one standard, kept in exactly
one place so it stays visible instead of spreading. Most projects keep
this file empty.

Each line names one exact member, written as `TypeName.member_name`, so
an unrelated member elsewhere with the same short name is never
accidentally covered.
