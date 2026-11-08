using System.Text;

namespace MdViewer.Render.Text;

/// <summary>
/// Pragmatic TeX → Unicode conversion so formulas stay readable in a native
/// renderer. Full typesetting (fractions, matrices) degrades to inline forms
/// such as <c>(a)/(b)</c> rather than failing.
/// </summary>
public static class MathText
{
    private static readonly Dictionary<string, string> Symbols = new(StringComparer.Ordinal)
    {
        ["alpha"] = "α", ["beta"] = "β", ["gamma"] = "γ", ["delta"] = "δ", ["epsilon"] = "ε",
        ["varepsilon"] = "ε", ["zeta"] = "ζ", ["eta"] = "η", ["theta"] = "θ", ["vartheta"] = "ϑ",
        ["iota"] = "ι", ["kappa"] = "κ", ["lambda"] = "λ", ["mu"] = "μ", ["nu"] = "ν",
        ["xi"] = "ξ", ["pi"] = "π", ["varpi"] = "ϖ", ["rho"] = "ρ", ["varrho"] = "ϱ",
        ["sigma"] = "σ", ["varsigma"] = "ς", ["tau"] = "τ", ["upsilon"] = "υ", ["phi"] = "φ",
        ["varphi"] = "φ", ["chi"] = "χ", ["psi"] = "ψ", ["omega"] = "ω",
        ["Gamma"] = "Γ", ["Delta"] = "Δ", ["Theta"] = "Θ", ["Lambda"] = "Λ", ["Xi"] = "Ξ",
        ["Pi"] = "Π", ["Sigma"] = "Σ", ["Upsilon"] = "Υ", ["Phi"] = "Φ", ["Psi"] = "Ψ", ["Omega"] = "Ω",
        ["times"] = "×", ["div"] = "÷", ["pm"] = "±", ["mp"] = "∓", ["cdot"] = "·", ["ast"] = "∗",
        ["le"] = "≤", ["leq"] = "≤", ["ge"] = "≥", ["geq"] = "≥", ["ne"] = "≠", ["neq"] = "≠",
        ["approx"] = "≈", ["equiv"] = "≡", ["sim"] = "∼", ["propto"] = "∝", ["infty"] = "∞",
        ["partial"] = "∂", ["nabla"] = "∇", ["sum"] = "∑", ["prod"] = "∏", ["int"] = "∫",
        ["iint"] = "∬", ["iiint"] = "∭", ["oint"] = "∮", ["sqrt"] = "√",
        ["in"] = "∈", ["notin"] = "∉", ["subset"] = "⊂", ["supset"] = "⊃", ["subseteq"] = "⊆",
        ["cup"] = "∪", ["cap"] = "∩", ["emptyset"] = "∅", ["varnothing"] = "∅",
        ["forall"] = "∀", ["exists"] = "∃", ["neg"] = "¬", ["land"] = "∧", ["lor"] = "∨",
        ["to"] = "→", ["rightarrow"] = "→", ["leftarrow"] = "←", ["Rightarrow"] = "⇒",
        ["leftrightarrow"] = "↔", ["mapsto"] = "↦", ["uparrow"] = "↑", ["downarrow"] = "↓",
        ["cdots"] = "⋯", ["ldots"] = "…", ["dots"] = "…", ["vdots"] = "⋮", ["ddots"] = "⋱",
        ["angle"] = "∠", ["perp"] = "⊥", ["parallel"] = "∥", ["triangle"] = "△",
        ["star"] = "⋆", ["circ"] = "∘", ["bullet"] = "•", ["oplus"] = "⊕", ["otimes"] = "⊗",
        ["ell"] = "ℓ", ["hbar"] = "ℏ", ["Re"] = "ℜ", ["Im"] = "ℑ", ["aleph"] = "ℵ",
        ["degree"] = "°", ["prime"] = "′", ["therefore"] = "∴", ["because"] = "∵",
        ["lfloor"] = "⌊", ["rfloor"] = "⌋", ["lceil"] = "⌈", ["rceil"] = "⌉",
        ["langle"] = "⟨", ["rangle"] = "⟩", ["mid"] = "∣", ["vert"] = "|", ["Vert"] = "‖",
        ["quad"] = " ", ["qquad"] = "  ", ["!"] = string.Empty, [","] = string.Empty, [";"] = " ",
        [":"] = " ", [" "] = " ",
    };

    private static readonly Dictionary<char, char> Superscripts = new()
    {
        ['0'] = '⁰', ['1'] = '¹', ['2'] = '²', ['3'] = '³', ['4'] = '⁴', ['5'] = '⁵', ['6'] = '⁶',
        ['7'] = '⁷', ['8'] = '⁸', ['9'] = '⁹', ['+'] = '⁺', ['-'] = '⁻', ['='] = '⁼', ['('] = '⁽',
        [')'] = '⁾', ['n'] = 'ⁿ', ['i'] = 'ⁱ', ['a'] = 'ᵃ', ['b'] = 'ᵇ', ['c'] = 'ᶜ', ['d'] = 'ᵈ',
        ['e'] = 'ᵉ', ['f'] = 'ᶠ', ['g'] = 'ᵍ', ['h'] = 'ʰ', ['j'] = 'ʲ', ['k'] = 'ᵏ', ['l'] = 'ˡ',
        ['m'] = 'ᵐ', ['o'] = 'ᵒ', ['p'] = 'ᵖ', ['r'] = 'ʳ', ['s'] = 'ˢ', ['t'] = 'ᵗ', ['u'] = 'ᵘ',
        ['v'] = 'ᵛ', ['w'] = 'ʷ', ['x'] = 'ˣ', ['y'] = 'ʸ', ['z'] = 'ᶻ', ['T'] = 'ᵀ', ['A'] = 'ᴬ',
        ['B'] = 'ᴮ', ['D'] = 'ᴰ', ['E'] = 'ᴱ', ['K'] = 'ᴷ', ['M'] = 'ᴹ', ['N'] = 'ᴺ', ['P'] = 'ᴾ',
        ['S'] = 'ˢ', ['W'] = 'ᵂ', ['α'] = 'ᵅ', ['β'] = 'ᵝ', ['γ'] = 'ᵞ', ['δ'] = 'ᵟ', ['π'] = 'ᵖ',
    };

    private static readonly Dictionary<char, char> Subscripts = new()
    {
        ['0'] = '₀', ['1'] = '₁', ['2'] = '₂', ['3'] = '₃', ['4'] = '₄', ['5'] = '₅', ['6'] = '₆',
        ['7'] = '₇', ['8'] = '₈', ['9'] = '₉', ['+'] = '₊', ['-'] = '₋', ['='] = '₌', ['('] = '₍',
        [')'] = '₎', ['a'] = 'ₐ', ['e'] = 'ₑ', ['h'] = 'ₕ', ['i'] = 'ᵢ', ['j'] = 'ⱼ', ['k'] = 'ₖ',
        ['l'] = 'ₗ', ['m'] = 'ₘ', ['n'] = 'ₙ', ['o'] = 'ₒ', ['p'] = 'ₚ', ['r'] = 'ᵣ', ['s'] = 'ₛ',
        ['t'] = 'ₜ', ['u'] = 'ᵤ', ['v'] = 'ᵥ', ['x'] = 'ₓ', ['β'] = 'ᵦ', ['γ'] = 'ᵧ', ['ρ'] = 'ᵨ',
        ['φ'] = 'ᵩ', ['χ'] = 'ᵪ',
    };

    public static string ToDisplay(string tex)
    {
        if (string.IsNullOrEmpty(tex))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(tex.Length);
        int i = 0;
        while (i < tex.Length)
        {
            char c = tex[i];

            if (c == '\\')
            {
                int start = ++i;
                if (i < tex.Length && !char.IsLetter(tex[i]))
                {
                    string single = tex[i..(i + 1)];
                    sb.Append(Symbols.TryGetValue(single, out string? mapped) ? mapped : single);
                    i++;
                    continue;
                }

                while (i < tex.Length && char.IsLetter(tex[i]))
                {
                    i++;
                }

                string command = tex[start..i];
                switch (command)
                {
                    case "frac" or "dfrac" or "tfrac":
                    {
                        string numerator = ReadGroup(tex, ref i);
                        string denominator = ReadGroup(tex, ref i);
                        sb.Append('(').Append(ToDisplay(numerator)).Append(")/(").Append(ToDisplay(denominator)).Append(')');
                        continue;
                    }

                    case "sqrt":
                    {
                        string radicand = ReadGroup(tex, ref i);
                        sb.Append('√').Append('(').Append(ToDisplay(radicand)).Append(')');
                        continue;
                    }

                    case "text" or "mathrm" or "mathit" or "mathbf" or "operatorname" or "textrm" or "ce":
                    {
                        string content = ReadGroup(tex, ref i);
                        sb.Append(StripCommands(content));
                        continue;
                    }

                    case "left" or "right" or "big" or "Big" or "bigl" or "bigr":
                    {
                        if (i < tex.Length && !char.IsLetterOrDigit(tex[i]))
                        {
                            sb.Append(tex[i]);
                            i++;
                        }

                        continue;
                    }

                    case "overline" or "bar" or "hat" or "vec" or "dot" or "tilde" or "underline":
                    {
                        string content = ReadGroup(tex, ref i);
                        sb.Append(ToDisplay(content));
                        continue;
                    }

                    default:
                        sb.Append(Symbols.TryGetValue(command, out string? symbol) ? symbol : command);
                        continue;
                }
            }

            if (c == '^' || c == '_')
            {
                string group = ReadGroup(tex, ref i);
                Dictionary<char, char> map = c == '^' ? Superscripts : Subscripts;
                string converted = ConvertGroup(group, map);
                if (converted.Length > 0)
                {
                    sb.Append(converted);
                }
                else
                {
                    sb.Append(c == '^' ? '^' : '_').Append(group);
                }

                continue;
            }

            if (c is '{' or '}')
            {
                i++;
                continue;
            }

            if (c == '&')
            {
                sb.Append("  ");
                i++;
                continue;
            }

            if (c == '~')
            {
                sb.Append('\u00A0');
                i++;
                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString().Trim();
    }

    private static string ConvertGroup(string group, Dictionary<char, char> map)
    {
        var sb = new StringBuilder(group.Length);
        foreach (char c in group)
        {
            if (map.TryGetValue(c, out char mapped))
            {
                sb.Append(mapped);
                continue;
            }

            if (c == ' ')
            {
                continue;
            }

            return string.Empty;
        }

        return sb.ToString();
    }

    private static string ReadGroup(string tex, ref int i)
    {
        if (i >= tex.Length)
        {
            return string.Empty;
        }

        if (tex[i] == '{')
        {
            int depth = 0;
            int start = i + 1;
            for (int j = i; j < tex.Length; j++)
            {
                if (tex[j] == '{')
                {
                    depth++;
                }
                else if (tex[j] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        i = j + 1;
                        return tex[start..j];
                    }
                }
            }

            i = tex.Length;
            return tex[start..];
        }

        if (tex[i] == '\\')
        {
            int start = i;
            i++;
            while (i < tex.Length && char.IsLetter(tex[i]))
            {
                i++;
            }

            return tex[start..i];
        }

        string single = tex[i..(i + 1)];
        i++;
        return single;
    }

    private static string StripCommands(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\\')
            {
                sb.Append(text[i]);
                continue;
            }

            int start = ++i;
            while (i < text.Length && char.IsLetter(text[i]))
            {
                i++;
            }

            if (i < text.Length && text[i] == ' ')
            {
                sb.Append(' ');
            }

            _ = start;
        }

        return sb.ToString();
    }
}

