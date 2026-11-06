namespace BaseStorage.Core.Compendium;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One line of a page, which may link to another page.</summary>
public sealed class CompendiumLine
{
    /// <summary>Initializes a new instance of the <see cref="CompendiumLine"/> class.</summary>
    /// <param name="text">The line's text.</param>
    /// <param name="link">The linked page's id, or null.</param>
    public CompendiumLine(string text, string? link = null)
    {
        Text = text;
        Link = link;
    }

    /// <summary>Gets the line's text.</summary>
    public string Text { get; }

    /// <summary>Gets the linked page's id, or null.</summary>
    public string? Link { get; }
}

/// <summary>A headed part of a page.</summary>
public sealed class CompendiumSection
{
    /// <summary>Initializes a new instance of the <see cref="CompendiumSection"/> class.</summary>
    /// <param name="heading">The heading.</param>
    /// <param name="lines">The lines.</param>
    public CompendiumSection(string heading, IReadOnlyList<CompendiumLine> lines)
    {
        Heading = heading;
        Lines = lines;
    }

    /// <summary>Gets the heading.</summary>
    public string Heading { get; }

    /// <summary>Gets the lines.</summary>
    public IReadOnlyList<CompendiumLine> Lines { get; }
}

/// <summary>One page of the compendium.</summary>
public sealed class CompendiumPage
{
    private readonly string searchText;

    /// <summary>Initializes a new instance of the <see cref="CompendiumPage"/> class.</summary>
    /// <param name="id">The page's id.</param>
    /// <param name="kind">The kind.</param>
    /// <param name="title">The title.</param>
    /// <param name="group">The group it's listed under.</param>
    /// <param name="description">The description.</param>
    /// <param name="sections">The sections.</param>
    public CompendiumPage(string id, CompendiumKind kind, string title, string group, string description, IReadOnlyList<CompendiumSection> sections)
    {
        Id = id;
        Kind = kind;
        Title = title;
        Group = group;
        Description = description;
        Sections = sections;
        searchText = (title + " " + group + " " + description).ToLowerInvariant();
    }

    /// <summary>Gets the page's id.</summary>
    public string Id { get; }

    /// <summary>Gets the kind.</summary>
    public CompendiumKind Kind { get; }

    /// <summary>Gets the title.</summary>
    public string Title { get; }

    /// <summary>Gets the group it's listed under.</summary>
    public string Group { get; }

    /// <summary>Gets the description.</summary>
    public string Description { get; }

    /// <summary>Gets the sections.</summary>
    public IReadOnlyList<CompendiumSection> Sections { get; }

    /// <summary>Tells whether every word appears in the title, group or description.</summary>
    /// <param name="words">The lower-case words.</param>
    /// <returns>True on a match.</returns>
    internal bool Matches(string[] words)
    {
        for (int i = 0; i < words.Length; i++)
        {
            if (searchText.IndexOf(words[i], StringComparison.Ordinal) < 0)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Every page, ready to look up and search.</summary>
public sealed class CompendiumBook
{
    private readonly Dictionary<string, CompendiumPage> pages;

    /// <summary>Initializes a new instance of the <see cref="CompendiumBook"/> class.</summary>
    /// <param name="pages">The pages; the first page with an id wins.</param>
    public CompendiumBook(IEnumerable<CompendiumPage> pages)
    {
        this.pages = new Dictionary<string, CompendiumPage>(StringComparer.Ordinal);
        foreach (CompendiumPage page in pages)
        {
            if (!this.pages.ContainsKey(page.Id))
            {
                this.pages.Add(page.Id, page);
            }
        }
    }

    /// <summary>Gets the number of pages.</summary>
    public int Count => pages.Count;

    /// <summary>Finds a page.</summary>
    /// <param name="id">The page's id.</param>
    /// <returns>The page, or null.</returns>
    public CompendiumPage? Find(string? id) => id != null && pages.TryGetValue(id, out CompendiumPage? page) ? page : null;

    /// <summary>Lists one kind's pages that match a search. Titles starting with the search come first, then the rest; each part is alphabetical, except biomes, which keep their journey order.</summary>
    /// <param name="kind">The kind.</param>
    /// <param name="query">The search, which may be empty.</param>
    /// <returns>The pages.</returns>
    public IReadOnlyList<CompendiumPage> Search(CompendiumKind kind, string? query)
    {
        string text = (query ?? string.Empty).Trim().ToLowerInvariant();
        string[] words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<CompendiumPage> matches = pages.Values.Where(p => p.Kind == kind && p.Matches(words));
        if (kind == CompendiumKind.Biome && words.Length == 0)
        {
            return matches.ToList();
        }

        return matches
            .OrderBy(p => text.Length > 0 && p.Title.StartsWith(text, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Id, StringComparer.Ordinal)
            .ToList();
    }
}
