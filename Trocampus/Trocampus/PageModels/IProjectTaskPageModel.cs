using CommunityToolkit.Mvvm.Input;
using Trocampus.Models;

namespace Trocampus.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}