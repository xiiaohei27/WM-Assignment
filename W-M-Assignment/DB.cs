using System.Collections.ObjectModel;
using System.Text.Json;

namespace Demo;

public record Todo(string Task, DateTime Date);

public class DB
{
    // TODO

    public DB()
    {
        // TODO
    }

    private void Load()
    {
        // TODO
    }

    private void Save()
    {
        // TODO
    }

    public ObservableCollection<Todo> GetTodos()
    {
        // TODO
        return [];
    }

    public void AddTodo(Todo todo)
    {
        // TODO
    }

    public void DeleteTodo(Todo todo)
    {
        // TODO
    }

    public void DeleteAllTodos()
    {
        // TODO
    }
}
