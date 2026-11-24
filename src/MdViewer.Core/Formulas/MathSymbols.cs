namespace MdViewer.Core.Formulas;

/// <summary>
/// Command tables: symbols, function names, accents and environment fences.
/// The symbol set follows KaTeX/MathJax (the union of both), so a document written for
/// either engine parses here without its own vocabulary.
/// </summary>
public static class MathSymbols
{
    private static readonly Dictionary<string, (string Text, MathAtomKind Kind)> Table = new(StringComparer.Ordinal)
    {
        // ------------------------------------------------------------------ Greek
        ["alpha"] = ("α", MathAtomKind.Ordinary), ["beta"] = ("β", MathAtomKind.Ordinary),
        ["gamma"] = ("γ", MathAtomKind.Ordinary), ["delta"] = ("δ", MathAtomKind.Ordinary),
        ["epsilon"] = ("ϵ", MathAtomKind.Ordinary), ["varepsilon"] = ("ε", MathAtomKind.Ordinary),
        ["zeta"] = ("ζ", MathAtomKind.Ordinary), ["eta"] = ("η", MathAtomKind.Ordinary),
        ["theta"] = ("θ", MathAtomKind.Ordinary), ["vartheta"] = ("ϑ", MathAtomKind.Ordinary),
        ["iota"] = ("ι", MathAtomKind.Ordinary), ["kappa"] = ("κ", MathAtomKind.Ordinary),
        ["varkappa"] = ("ϰ", MathAtomKind.Ordinary),
        ["lambda"] = ("λ", MathAtomKind.Ordinary), ["mu"] = ("μ", MathAtomKind.Ordinary),
        ["nu"] = ("ν", MathAtomKind.Ordinary), ["xi"] = ("ξ", MathAtomKind.Ordinary),
        ["omicron"] = ("ο", MathAtomKind.Ordinary),
        ["pi"] = ("π", MathAtomKind.Ordinary), ["varpi"] = ("ϖ", MathAtomKind.Ordinary),
        ["rho"] = ("ρ", MathAtomKind.Ordinary), ["varrho"] = ("ϱ", MathAtomKind.Ordinary),
        ["sigma"] = ("σ", MathAtomKind.Ordinary), ["varsigma"] = ("ς", MathAtomKind.Ordinary),
        ["tau"] = ("τ", MathAtomKind.Ordinary), ["upsilon"] = ("υ", MathAtomKind.Ordinary),
        ["phi"] = ("ϕ", MathAtomKind.Ordinary), ["varphi"] = ("φ", MathAtomKind.Ordinary),
        ["chi"] = ("χ", MathAtomKind.Ordinary), ["psi"] = ("ψ", MathAtomKind.Ordinary),
        ["omega"] = ("ω", MathAtomKind.Ordinary),
        ["digamma"] = ("ϝ", MathAtomKind.Ordinary),
        // Greek, uppercase: upright by convention
        ["Gamma"] = ("Γ", MathAtomKind.Ordinary), ["Delta"] = ("Δ", MathAtomKind.Ordinary),
        ["Theta"] = ("Θ", MathAtomKind.Ordinary), ["Lambda"] = ("Λ", MathAtomKind.Ordinary),
        ["Xi"] = ("Ξ", MathAtomKind.Ordinary), ["Pi"] = ("Π", MathAtomKind.Ordinary),
        ["Sigma"] = ("Σ", MathAtomKind.Ordinary), ["Upsilon"] = ("Υ", MathAtomKind.Ordinary),
        ["Phi"] = ("Φ", MathAtomKind.Ordinary), ["Psi"] = ("Ψ", MathAtomKind.Ordinary),
        ["Omega"] = ("Ω", MathAtomKind.Ordinary),
        // ------------------------------------------------------------------ binary
        ["pm"] = ("±", MathAtomKind.Binary), ["mp"] = ("∓", MathAtomKind.Binary),
        ["times"] = ("×", MathAtomKind.Binary), ["div"] = ("÷", MathAtomKind.Binary),
        ["cdot"] = ("⋅", MathAtomKind.Binary), ["cdotp"] = ("·", MathAtomKind.Binary),
        ["centerdot"] = ("·", MathAtomKind.Binary),
        ["ast"] = ("∗", MathAtomKind.Binary), ["star"] = ("⋆", MathAtomKind.Binary),
        ["circ"] = ("∘", MathAtomKind.Binary), ["bullet"] = ("∙", MathAtomKind.Binary),
        ["oplus"] = ("⊕", MathAtomKind.Binary), ["ominus"] = ("⊖", MathAtomKind.Binary),
        ["otimes"] = ("⊗", MathAtomKind.Binary), ["oslash"] = ("⊘", MathAtomKind.Binary),
        ["odot"] = ("⊙", MathAtomKind.Binary), ["circledcirc"] = ("⊚", MathAtomKind.Binary),
        ["circleddash"] = ("⊝", MathAtomKind.Binary), ["circledast"] = ("⊛", MathAtomKind.Binary),
        ["cup"] = ("∪", MathAtomKind.Binary), ["cap"] = ("∩", MathAtomKind.Binary),
        ["cupdot"] = ("⊍", MathAtomKind.Binary), ["capdot"] = ("⩀", MathAtomKind.Binary),
        ["setminus"] = ("∖", MathAtomKind.Binary), ["smallsetminus"] = ("∖", MathAtomKind.Binary),
        ["wedge"] = ("∧", MathAtomKind.Binary), ["land"] = ("∧", MathAtomKind.Binary),
        ["vee"] = ("∨", MathAtomKind.Binary), ["lor"] = ("∨", MathAtomKind.Binary),
        ["barwedge"] = ("⊼", MathAtomKind.Binary), ["veebar"] = ("⊻", MathAtomKind.Binary),
        ["curlywedge"] = ("⋏", MathAtomKind.Binary), ["curlyvee"] = ("⋎", MathAtomKind.Binary),
        ["doublebarwedge"] = ("⩞", MathAtomKind.Binary),
        ["dagger"] = ("†", MathAtomKind.Binary), ["ddagger"] = ("‡", MathAtomKind.Binary),
        ["uplus"] = ("⊎", MathAtomKind.Binary), ["sqcup"] = ("⊔", MathAtomKind.Binary),
        ["sqcap"] = ("⊓", MathAtomKind.Binary), ["amalg"] = ("⨿", MathAtomKind.Binary),
        ["diamond"] = ("⋄", MathAtomKind.Binary), ["bigtriangleup"] = ("△", MathAtomKind.Binary),
        ["bigtriangledown"] = ("▽", MathAtomKind.Binary), ["triangleleft"] = ("◃", MathAtomKind.Binary),
        ["triangleright"] = ("▹", MathAtomKind.Binary),
        ["lhd"] = ("⊲", MathAtomKind.Binary), ["rhd"] = ("⊳", MathAtomKind.Binary),
        ["unlhd"] = ("⊴", MathAtomKind.Binary), ["unrhd"] = ("⊵", MathAtomKind.Binary),
        ["boxtimes"] = ("⊠", MathAtomKind.Binary), ["boxplus"] = ("⊞", MathAtomKind.Binary),
        ["boxminus"] = ("⊟", MathAtomKind.Binary), ["boxdot"] = ("⊡", MathAtomKind.Binary),
        ["ltimes"] = ("⋉", MathAtomKind.Binary), ["rtimes"] = ("⋊", MathAtomKind.Binary),
        ["leftthreetimes"] = ("⋋", MathAtomKind.Binary), ["rightthreetimes"] = ("⋌", MathAtomKind.Binary),
        ["divideontimes"] = ("⋇", MathAtomKind.Binary), ["intercal"] = ("⊺", MathAtomKind.Binary),
        ["wr"] = ("≀", MathAtomKind.Binary), ["dotplus"] = ("∔", MathAtomKind.Binary),
        // -------------------------------------------------------------- relations
        ["le"] = ("≤", MathAtomKind.Relation), ["leq"] = ("≤", MathAtomKind.Relation),
        ["ge"] = ("≥", MathAtomKind.Relation), ["geq"] = ("≥", MathAtomKind.Relation),
        ["leqq"] = ("≦", MathAtomKind.Relation), ["geqq"] = ("≧", MathAtomKind.Relation),
        ["leqslant"] = ("⩽", MathAtomKind.Relation), ["geqslant"] = ("⩾", MathAtomKind.Relation),
        ["ne"] = ("≠", MathAtomKind.Relation), ["neq"] = ("≠", MathAtomKind.Relation),
        ["approx"] = ("≈", MathAtomKind.Relation), ["approxeq"] = ("≊", MathAtomKind.Relation),
        ["cong"] = ("≅", MathAtomKind.Relation), ["equiv"] = ("≡", MathAtomKind.Relation),
        ["sim"] = ("∼", MathAtomKind.Relation), ["simeq"] = ("≃", MathAtomKind.Relation),
        ["backsim"] = ("∽", MathAtomKind.Relation), ["thicksim"] = ("∼", MathAtomKind.Relation),
        ["propto"] = ("∝", MathAtomKind.Relation), ["varpropto"] = ("∝", MathAtomKind.Relation),
        ["ll"] = ("≪", MathAtomKind.Relation), ["gg"] = ("≫", MathAtomKind.Relation),
        ["lll"] = ("⋘", MathAtomKind.Relation), ["ggg"] = ("⋙", MathAtomKind.Relation),
        ["doteq"] = ("≐", MathAtomKind.Relation), ["doteqdot"] = ("≑", MathAtomKind.Relation),
        ["fallingdotseq"] = ("≒", MathAtomKind.Relation), ["risingdotseq"] = ("≓", MathAtomKind.Relation),
        ["triangleq"] = ("≜", MathAtomKind.Relation), ["circeq"] = ("≗", MathAtomKind.Relation),
        ["eqcirc"] = ("≖", MathAtomKind.Relation), ["bumpeq"] = ("≏", MathAtomKind.Relation),
        ["Bumpeq"] = ("≎", MathAtomKind.Relation),
        ["prec"] = ("≺", MathAtomKind.Relation), ["succ"] = ("≻", MathAtomKind.Relation),
        ["preceq"] = ("⪯", MathAtomKind.Relation), ["succeq"] = ("⪰", MathAtomKind.Relation),
        ["precsim"] = ("≾", MathAtomKind.Relation), ["succsim"] = ("≿", MathAtomKind.Relation),
        ["precapprox"] = ("⪷", MathAtomKind.Relation), ["succapprox"] = ("⪸", MathAtomKind.Relation),
        ["subset"] = ("⊂", MathAtomKind.Relation), ["supset"] = ("⊃", MathAtomKind.Relation),
        ["subseteq"] = ("⊆", MathAtomKind.Relation), ["supseteq"] = ("⊇", MathAtomKind.Relation),
        ["subseteqq"] = ("⫅", MathAtomKind.Relation), ["supseteqq"] = ("⫆", MathAtomKind.Relation),
        ["subsetneq"] = ("⊊", MathAtomKind.Relation), ["supsetneq"] = ("⊋", MathAtomKind.Relation),
        ["nsubseteq"] = ("⊈", MathAtomKind.Relation), ["nsupseteq"] = ("⊉", MathAtomKind.Relation),
        ["sqsubset"] = ("⊏", MathAtomKind.Relation), ["sqsupset"] = ("⊐", MathAtomKind.Relation),
        ["sqsubseteq"] = ("⊑", MathAtomKind.Relation), ["sqsupseteq"] = ("⊒", MathAtomKind.Relation),
        ["in"] = ("∈", MathAtomKind.Relation), ["notin"] = ("∉", MathAtomKind.Relation),
        ["ni"] = ("∋", MathAtomKind.Relation), ["owns"] = ("∋", MathAtomKind.Relation),
        ["notni"] = ("∌", MathAtomKind.Relation),
        ["perp"] = ("⊥", MathAtomKind.Relation), ["parallel"] = ("∥", MathAtomKind.Relation),
        ["nparallel"] = ("∦", MathAtomKind.Relation), ["mid"] = ("∣", MathAtomKind.Relation),
        ["nmid"] = ("∤", MathAtomKind.Relation), ["shortmid"] = ("∣", MathAtomKind.Relation),
        ["shortparallel"] = ("∥", MathAtomKind.Relation),
        ["models"] = ("⊨", MathAtomKind.Relation), ["vdash"] = ("⊢", MathAtomKind.Relation),
        ["dashv"] = ("⊣", MathAtomKind.Relation), ["vDash"] = ("⊨", MathAtomKind.Relation),
        ["Vdash"] = ("⊩", MathAtomKind.Relation), ["Vvdash"] = ("⊪", MathAtomKind.Relation),
        ["nvdash"] = ("⊬", MathAtomKind.Relation), ["nvDash"] = ("⊭", MathAtomKind.Relation),
        ["top"] = ("⊤", MathAtomKind.Ordinary), ["bot"] = ("⊥", MathAtomKind.Ordinary),
        ["smile"] = ("⌣", MathAtomKind.Relation), ["frown"] = ("⌢", MathAtomKind.Relation),
        ["between"] = ("≬", MathAtomKind.Relation), ["pitchfork"] = ("⋔", MathAtomKind.Relation),
        ["lessgtr"] = ("≶", MathAtomKind.Relation), ["gtrless"] = ("≷", MathAtomKind.Relation),
        ["lesseqgtr"] = ("⋚", MathAtomKind.Relation), ["gtreqless"] = ("⋛", MathAtomKind.Relation),
        ["trianglelefteq"] = ("⊴", MathAtomKind.Relation), ["trianglerighteq"] = ("⊵", MathAtomKind.Relation),
        ["bowtie"] = ("⋈", MathAtomKind.Relation),
        // ------------------------------------------------------------------ arrows
        ["to"] = ("→", MathAtomKind.Relation), ["rightarrow"] = ("→", MathAtomKind.Relation),
        ["leftarrow"] = ("←", MathAtomKind.Relation), ["gets"] = ("←", MathAtomKind.Relation),
        ["leftrightarrow"] = ("↔", MathAtomKind.Relation),
        ["Rightarrow"] = ("⇒", MathAtomKind.Relation), ["Leftarrow"] = ("⇐", MathAtomKind.Relation),
        ["Leftrightarrow"] = ("⇔", MathAtomKind.Relation),
        ["implies"] = ("⟹", MathAtomKind.Relation), ["impliedby"] = ("⟸", MathAtomKind.Relation),
        ["iff"] = ("⟺", MathAtomKind.Relation),
        ["longrightarrow"] = ("⟶", MathAtomKind.Relation), ["longleftarrow"] = ("⟵", MathAtomKind.Relation),
        ["longleftrightarrow"] = ("⟷", MathAtomKind.Relation),
        ["Longrightarrow"] = ("⟹", MathAtomKind.Relation), ["Longleftarrow"] = ("⟸", MathAtomKind.Relation),
        ["Longleftrightarrow"] = ("⟺", MathAtomKind.Relation),
        ["mapsto"] = ("↦", MathAtomKind.Relation), ["longmapsto"] = ("⟼", MathAtomKind.Relation),
        ["hookrightarrow"] = ("↪", MathAtomKind.Relation), ["hookleftarrow"] = ("↩", MathAtomKind.Relation),
        ["nearrow"] = ("↗", MathAtomKind.Relation), ["searrow"] = ("↘", MathAtomKind.Relation),
        ["upuparrows"] = ("⇈", MathAtomKind.Relation), ["downdownarrows"] = ("⇊", MathAtomKind.Relation),
        ["swarrow"] = ("↙", MathAtomKind.Relation), ["nwarrow"] = ("↖", MathAtomKind.Relation),
        ["uparrow"] = ("↑", MathAtomKind.Relation), ["downarrow"] = ("↓", MathAtomKind.Relation),
        ["updownarrow"] = ("↕", MathAtomKind.Relation),
        ["Uparrow"] = ("⇑", MathAtomKind.Relation), ["Downarrow"] = ("⇓", MathAtomKind.Relation),
        ["Updownarrow"] = ("⇕", MathAtomKind.Relation),
        ["rightharpoonup"] = ("⇀", MathAtomKind.Relation), ["rightharpoondown"] = ("⇁", MathAtomKind.Relation),
        ["leftharpoonup"] = ("↼", MathAtomKind.Relation), ["leftharpoondown"] = ("↽", MathAtomKind.Relation),
        ["rightleftharpoons"] = ("⇌", MathAtomKind.Relation), ["leftrightharpoons"] = ("⇋", MathAtomKind.Relation),
        ["rightrightarrows"] = ("⇉", MathAtomKind.Relation), ["leftleftarrows"] = ("⇇", MathAtomKind.Relation),
        ["rightleftarrows"] = ("⇄", MathAtomKind.Relation), ["leftrightarrows"] = ("⇆", MathAtomKind.Relation),
        ["curvearrowright"] = ("↷", MathAtomKind.Relation), ["curvearrowleft"] = ("↶", MathAtomKind.Relation),
        ["circlearrowright"] = ("↻", MathAtomKind.Relation), ["circlearrowleft"] = ("↺", MathAtomKind.Relation),
        ["dashrightarrow"] = ("⇢", MathAtomKind.Relation), ["dashleftarrow"] = ("⇠", MathAtomKind.Relation),
        ["twoheadrightarrow"] = ("↠", MathAtomKind.Relation), ["twoheadleftarrow"] = ("↞", MathAtomKind.Relation),
        ["rightarrowtail"] = ("↣", MathAtomKind.Relation), ["leftarrowtail"] = ("↢", MathAtomKind.Relation),
        ["nrightarrow"] = ("↛", MathAtomKind.Relation), ["nleftarrow"] = ("↚", MathAtomKind.Relation),
        ["nRightarrow"] = ("⇏", MathAtomKind.Relation), ["nLeftarrow"] = ("⇍", MathAtomKind.Relation),
        ["rightsquigarrow"] = ("⇝", MathAtomKind.Relation), ["leadsto"] = ("⇝", MathAtomKind.Relation),
        ["looparrowright"] = ("↬", MathAtomKind.Relation), ["looparrowleft"] = ("↫", MathAtomKind.Relation),
        ["Rsh"] = ("↱", MathAtomKind.Relation), ["Lsh"] = ("↰", MathAtomKind.Relation),
        ["multimap"] = ("⊸", MathAtomKind.Relation),
        // -------------------------------------------------------- large operators
        ["sum"] = ("∑", MathAtomKind.LargeOperator), ["prod"] = ("∏", MathAtomKind.LargeOperator),
        ["coprod"] = ("∐", MathAtomKind.LargeOperator),
        ["int"] = ("∫", MathAtomKind.LargeOperator), ["iint"] = ("∬", MathAtomKind.LargeOperator),
        ["iiint"] = ("∭", MathAtomKind.LargeOperator), ["iiiint"] = ("⨌", MathAtomKind.LargeOperator),
        ["oint"] = ("∮", MathAtomKind.LargeOperator), ["oiint"] = ("∯", MathAtomKind.LargeOperator),
        ["oiiint"] = ("∰", MathAtomKind.LargeOperator),
        ["smallint"] = ("∫", MathAtomKind.Ordinary),
        ["bigcup"] = ("⋃", MathAtomKind.LargeOperator), ["bigcap"] = ("⋂", MathAtomKind.LargeOperator),
        ["bigoplus"] = ("⨁", MathAtomKind.LargeOperator), ["bigotimes"] = ("⨂", MathAtomKind.LargeOperator),
        ["bigodot"] = ("⨀", MathAtomKind.LargeOperator), ["biguplus"] = ("⨄", MathAtomKind.LargeOperator),
        ["bigsqcup"] = ("⨆", MathAtomKind.LargeOperator),
        ["bigwedge"] = ("⋀", MathAtomKind.LargeOperator), ["bigvee"] = ("⋁", MathAtomKind.LargeOperator),
        ["bigstar"] = ("★", MathAtomKind.Ordinary),
        // ------------------------------------------------------------------- misc
        ["infty"] = ("∞", MathAtomKind.Ordinary), ["partial"] = ("∂", MathAtomKind.Ordinary),
        ["nabla"] = ("∇", MathAtomKind.Ordinary), ["forall"] = ("∀", MathAtomKind.Ordinary),
        ["exists"] = ("∃", MathAtomKind.Ordinary), ["nexists"] = ("∄", MathAtomKind.Ordinary),
        ["neg"] = ("¬", MathAtomKind.Ordinary), ["lnot"] = ("¬", MathAtomKind.Ordinary),
        ["emptyset"] = ("∅", MathAtomKind.Ordinary), ["varnothing"] = ("∅", MathAtomKind.Ordinary),
        ["aleph"] = ("ℵ", MathAtomKind.Ordinary), ["beth"] = ("ℶ", MathAtomKind.Ordinary),
        ["gimel"] = ("ℷ", MathAtomKind.Ordinary), ["daleth"] = ("ℸ", MathAtomKind.Ordinary),
        ["hbar"] = ("ℏ", MathAtomKind.Ordinary), ["hslash"] = ("ℏ", MathAtomKind.Ordinary),
        ["ell"] = ("ℓ", MathAtomKind.Ordinary), ["Re"] = ("ℜ", MathAtomKind.Ordinary),
        ["Im"] = ("ℑ", MathAtomKind.Ordinary), ["wp"] = ("℘", MathAtomKind.Ordinary),
        ["imath"] = ("ı", MathAtomKind.Ordinary), ["jmath"] = ("ȷ", MathAtomKind.Ordinary),
        ["angle"] = ("∠", MathAtomKind.Ordinary), ["measuredangle"] = ("∡", MathAtomKind.Ordinary),
        ["sphericalangle"] = ("∢", MathAtomKind.Ordinary),
        ["triangle"] = ("△", MathAtomKind.Ordinary), ["vartriangle"] = ("△", MathAtomKind.Ordinary),
        ["triangledown"] = ("▽", MathAtomKind.Ordinary),
        ["square"] = ("□", MathAtomKind.Ordinary), ["Box"] = ("□", MathAtomKind.Ordinary),
        ["blacksquare"] = ("■", MathAtomKind.Ordinary), ["squarebullet"] = ("▪", MathAtomKind.Ordinary),
        ["lozenge"] = ("◊", MathAtomKind.Ordinary), ["blacklozenge"] = ("◆", MathAtomKind.Ordinary),
        ["bigcirc"] = ("◯", MathAtomKind.Ordinary), ["circledS"] = ("Ⓢ", MathAtomKind.Ordinary),
        ["prime"] = ("′", MathAtomKind.Ordinary), ["backprime"] = ("‵", MathAtomKind.Ordinary),
        ["degree"] = ("°", MathAtomKind.Ordinary),
        ["therefore"] = ("∴", MathAtomKind.Relation), ["because"] = ("∵", MathAtomKind.Relation),
        ["ldots"] = ("…", MathAtomKind.Ordinary), ["dots"] = ("…", MathAtomKind.Ordinary),
        ["dotsc"] = ("…", MathAtomKind.Ordinary), ["dotsb"] = ("⋯", MathAtomKind.Ordinary),
        ["dotsm"] = ("⋯", MathAtomKind.Ordinary), ["dotsi"] = ("⋯", MathAtomKind.Ordinary),
        ["dotso"] = ("…", MathAtomKind.Ordinary),
        ["cdots"] = ("⋯", MathAtomKind.Ordinary), ["vdots"] = ("⋮", MathAtomKind.Ordinary),
        ["ddots"] = ("⋱", MathAtomKind.Ordinary), ["iddots"] = ("⋰", MathAtomKind.Ordinary),
        ["S"] = ("§", MathAtomKind.Ordinary), ["P"] = ("¶", MathAtomKind.Ordinary),
        ["copyright"] = ("©", MathAtomKind.Ordinary), ["circledR"] = ("®", MathAtomKind.Ordinary),
        ["pounds"] = ("£", MathAtomKind.Ordinary), ["yen"] = ("¥", MathAtomKind.Ordinary),
        ["checkmark"] = ("✓", MathAtomKind.Ordinary), ["maltese"] = ("✠", MathAtomKind.Ordinary),
        ["dag"] = ("†", MathAtomKind.Ordinary), ["ddag"] = ("‡", MathAtomKind.Ordinary),
        ["lnotx"] = ("¬", MathAtomKind.Ordinary),
        ["colon"] = (":", MathAtomKind.Punctuation),
        ["cdotsc"] = ("⋯", MathAtomKind.Ordinary),
        ["eth"] = ("ð", MathAtomKind.Ordinary), ["Finv"] = ("Ⅎ", MathAtomKind.Ordinary),
        ["Game"] = ("⅁", MathAtomKind.Ordinary), ["mho"] = ("℧", MathAtomKind.Ordinary),
        ["Bbbk"] = ("𝕜", MathAtomKind.Ordinary), ["complement"] = ("∁", MathAtomKind.Ordinary),
        ["ulcorner"] = ("⌜", MathAtomKind.Ordinary), ["urcorner"] = ("⌝", MathAtomKind.Ordinary),
        ["llcorner"] = ("⌞", MathAtomKind.Ordinary), ["lrcorner"] = ("⌟", MathAtomKind.Ordinary),
        ["backslash"] = ("∖", MathAtomKind.Ordinary),
        ["TeX"] = ("TeX", MathAtomKind.Ordinary), ["LaTeX"] = ("LaTeX", MathAtomKind.Ordinary),
        ["KaTeX"] = ("KaTeX", MathAtomKind.Ordinary), ["MathJax"] = ("MathJax", MathAtomKind.Ordinary),
        ["BibTeX"] = ("BibTeX", MathAtomKind.Ordinary),
        // ------------------------------------------------- punctuation and fences
        ["{"] = ("{", MathAtomKind.Open), ["}"] = ("}", MathAtomKind.Close),
        ["lbrace"] = ("{", MathAtomKind.Open), ["rbrace"] = ("}", MathAtomKind.Close),
        ["lbrack"] = ("[", MathAtomKind.Open), ["rbrack"] = ("]", MathAtomKind.Close),
        ["langle"] = ("⟨", MathAtomKind.Open), ["rangle"] = ("⟩", MathAtomKind.Close),
        ["lfloor"] = ("⌊", MathAtomKind.Open), ["rfloor"] = ("⌋", MathAtomKind.Close),
        ["lceil"] = ("⌈", MathAtomKind.Open), ["rceil"] = ("⌉", MathAtomKind.Close),
        ["lgroup"] = ("⟮", MathAtomKind.Open), ["rgroup"] = ("⟯", MathAtomKind.Close),
        ["lmoustache"] = ("⎰", MathAtomKind.Open), ["rmoustache"] = ("⎱", MathAtomKind.Close),
        ["lvert"] = ("|", MathAtomKind.Open), ["rvert"] = ("|", MathAtomKind.Close),
        ["lVert"] = ("‖", MathAtomKind.Open), ["rVert"] = ("‖", MathAtomKind.Close),
        ["vert"] = ("|", MathAtomKind.Ordinary), ["Vert"] = ("‖", MathAtomKind.Ordinary),
        ["#"] = ("#", MathAtomKind.Ordinary), ["&"] = ("&", MathAtomKind.Ordinary),
        ["%"] = ("%", MathAtomKind.Ordinary), ["_"] = ("_", MathAtomKind.Ordinary),
        ["$"] = ("$", MathAtomKind.Ordinary), ["|"] = ("‖", MathAtomKind.Ordinary),
        [":"] = (":", MathAtomKind.Punctuation),
        [">"] = (">", MathAtomKind.Relation), ["<"] = ("<", MathAtomKind.Relation),
    };

    private static readonly HashSet<string> Functions = new(StringComparer.Ordinal)
    {
        "sin", "cos", "tan", "cot", "sec", "csc", "arcsin", "arccos", "arctan", "arccot", "arcsec", "arccsc",
        "sinh", "cosh", "tanh", "coth", "sech", "csch", "arsinh", "arcosh", "artanh",
        "log", "ln", "lg", "exp", "det", "dim", "ker", "deg", "gcd", "hom", "arg",
        "lim", "liminf", "limsup", "max", "min", "sup", "inf", "Pr", "mod", "bmod",
        "sgn", "tr", "rank", "diag", "erf", "Res",
    };

    /// <summary>Limits are set under a display operator: \\lim, \\max, \\sum …</summary>
    private static readonly HashSet<string> LimitOperators = new(StringComparer.Ordinal)
    {
        "lim", "liminf", "limsup", "max", "min", "sup", "inf", "det", "gcd", "Pr", "injlim", "projlim",
    };

    private static readonly Dictionary<string, string> Accents = new(StringComparer.Ordinal)
    {
        ["hat"] = "\u02C6",
        ["widehat"] = "\u02C6",
        ["bar"] = "\u00AF",
        ["overline"] = "\u00AF",
        ["vec"] = "\u20D7",
        ["overrightarrow"] = "\u20D7",
        ["overleftarrow"] = "\u20D6",
        ["overleftrightarrow"] = "\u20E1",
        ["dot"] = "\u02D9",
        ["ddot"] = "\u00A8",
        ["dddot"] = "\u20DB",
        ["ddddot"] = "\u20DC",
        ["tilde"] = "\u02DC",
        ["widetilde"] = "\u02DC",
        ["check"] = "\u02C7",
        ["widecheck"] = "\u02C7",
        ["breve"] = "\u02D8",
        ["acute"] = "\u00B4",
        ["grave"] = "\u0060",
        ["mathring"] = "\u02DA",
        ["overbrace"] = "\u23DE",
        ["underbrace"] = "\u23DF",
        ["overparen"] = "\u23DC",
        ["underparen"] = "\u23DD",
        ["overgroup"] = "\u23E0",
        ["undergroup"] = "\u23E1",
        ["overbracket"] = "\u23B4",
        ["underbracket"] = "\u23B5",
    };

    /// <summary>Accents that draw a rule instead of a glyph (TeX's \overline, \underline).</summary>
    public static bool IsRuleAccent(string command) => command is "overline" or "underline" or "bar";

    public static bool TryGet(string command, out string text, out MathAtomKind kind)
    {
        if (Table.TryGetValue(command, out (string Text, MathAtomKind Kind) entry))
        {
            text = entry.Text;
            kind = entry.Kind;
            return true;
        }

        text = string.Empty;
        kind = MathAtomKind.Ordinary;
        return false;
    }

    public static bool IsFunction(string command) => Functions.Contains(command);

    public static bool TakesLimits(string command) => LimitOperators.Contains(command);

    public static bool TryGetAccent(string command, out string mark, out bool under)
    {
        under = command is "underline" or "underbrace" or "underparen" or "undergroup" or "underbracket";
        if (under)
        {
            mark = command switch
            {
                "underbrace" => "\u23DF",
                "underparen" => "\u23DD",
                "undergroup" => "\u23E1",
                "underbracket" => "\u23B5",
                _ => "\u005F",
            };
            return true;
        }

        return Accents.TryGetValue(command, out mark!);
    }

    /// <summary>Fences used by \\left … \\right and by the matrix environments.</summary>
    public static bool IsDelimiter(string text) => text is
        "(" or ")" or "[" or "]" or "{" or "}" or "|" or "‖" or "/" or "∖" or
        "⟨" or "⟩" or "⌊" or "⌋" or "⌈" or "⌉" or "⟮" or "⟯" or "⎰" or "⎱" or
        "." or "" or "↑" or "↓" or "↕" or "⇑" or "⇓" or "⇕" or "||";

    public static string Blackboard(char c) => c switch
    {
        >= 'A' and <= 'Z' => char.ConvertFromUtf32(0x1D538 + (c - 'A')),
        >= 'a' and <= 'z' => char.ConvertFromUtf32(0x1D552 + (c - 'a')),
        >= '0' and <= '9' => char.ConvertFromUtf32(0x1D7D8 + (c - '0')),
        _ => c.ToString(),
    };

    public static string Script(char c) => c switch
    {
        >= 'A' and <= 'Z' => char.ConvertFromUtf32(0x1D49C + (c - 'A')),
        >= 'a' and <= 'z' => char.ConvertFromUtf32(0x1D4B6 + (c - 'a')),
        _ => c.ToString(),
    };

    public static string Fraktur(char c) => c switch
    {
        >= 'A' and <= 'Z' => char.ConvertFromUtf32(0x1D504 + (c - 'A')),
        >= 'a' and <= 'z' => char.ConvertFromUtf32(0x1D51E + (c - 'a')),
        _ => c.ToString(),
    };

    /// <summary>Sans-serif and monospace need no table: the face itself changes.</summary>
    public static string DoubleStruck(char c) => Blackboard(c);

    /// <summary>Matrix-like environments: fence pair and whether cells are left aligned.</summary>
    public static bool TryGetEnvironment(string name, out string open, out string close, out bool leftAligned)
    {
        leftAligned = false;
        (open, close) = name switch
        {
            "matrix" or "matrix*" => ("", ""),
            "smallmatrix" => ("", ""),
            "pmatrix" or "pmatrix*" => ("(", ")"),
            "bmatrix" or "bmatrix*" => ("[", "]"),
            "Bmatrix" or "Bmatrix*" => ("{", "}"),
            "vmatrix" or "vmatrix*" => ("|", "|"),
            "Vmatrix" or "Vmatrix*" => ("‖", "‖"),
            "cases" or "cases*" => ("{", ""),
            "dcases" or "dcases*" => ("{", ""),
            "rcases" or "rcases*" => ("", "}"),
            "aligned" or "alignedat" => ("", ""),
            "align" or "align*" => ("", ""),
            "gather" or "gather*" or "gathered" => ("", ""),
            "split" or "multline" or "multline*" => ("", ""),
            "equation" or "equation*" or "displaylines" or "eqnarray" or "eqnarray*" => ("", ""),
            "array" or "subarray" or "darray" => ("", ""),
            "CD" => ("", ""),
            _ => (null!, null!),
        };

        leftAligned = name is "cases" or "cases*" or "dcases" or "dcases*" or "rcases" or "rcases*"
            or "aligned" or "alignedat" or "align" or "align*" or "array" or "subarray" or "darray"
            or "split" or "displaylines";

        return open is not null;
    }
}
