using Darkages.Types;

using System.Diagnostics.CodeAnalysis;

namespace Darkages.Dialogs.Abstractions;

public interface ISkillTeacherSource : IDialogSourceEntity
{
    ICollection<Skill> SkillsToTeach { get; }
    bool TryGetSkill(string skillName, [MaybeNullWhen(false)] out Skill skill);
}