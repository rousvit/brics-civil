namespace BricsLayerPlugin.Models
{
    /// <summary>
    /// Hladina (Level) ve stylu Vectorworks Design Layer.
    /// Virtuální organizační systém nad BricsCAD entitami.
    /// Řídí pořadí zobrazení (draw order) a hromadnou viditelnost objektů.
    /// Entita patří do právě jedné hladiny (uloženo přes XData).
    /// </summary>
    public class VwLevel
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Stav hladiny (On/Off/Grayed).</summary>
        public VwLevelState State { get; set; } = VwLevelState.On;

        /// <summary>
        /// Pořadí hladiny – nižší číslo = pod, vyšší = nahoře.
        /// Objekty v hladině s vyšším pořadím se vykreslují přes nižší.
        /// </summary>
        public int Order { get; set; }

        /// <summary>Je tato hladina aktivní? Nové objekty se přiřadí do ní.</summary>
        public bool IsActive { get; set; }

        /// <summary>Uživatelský popis.</summary>
        public string Description { get; set; } = string.Empty;

        public override string ToString() => $"{Order}: {Name} [{State}]";
    }

    public enum VwLevelState
    {
        /// <summary>Hladina je viditelná a editovatelná.</summary>
        On,
        /// <summary>Hladina je neviditelná (objekty skryté).</summary>
        Off,
        /// <summary>Hladina je viditelná, ale zašedlá (nelze editovat).</summary>
        Grayed
    }
}
