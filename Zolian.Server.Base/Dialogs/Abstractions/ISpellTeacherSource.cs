using Darkages.Types;

using System.Diagnostics.CodeAnalysis;

namespace Darkages.Dialogs.Abstractions;

public interface ISpellTeacherSource : IDialogSourceEntity
{
    ICollection<Spell> SpellsToTeach { get; }
    bool TryGetSpell(string spellName, [MaybeNullWhen(false)] out Spell spell);
}