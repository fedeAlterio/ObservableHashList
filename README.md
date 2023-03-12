# Intro

Consider a simple MVVM ViewModel that polls some data every 5 seconds. For example:
```csharp
internal class MainWindowViewModel : ObservableObject
{
    CancellationTokenSource _cancellationTokenSource = new();
    public MainWindowViewModel() => _ = Poll();
    public ObservableCollection<Person> People { get; } = new();

    async Task Poll()
    {
        var token = _cancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            var people = await FetchPeople(token);
            People.Clear();
            foreach (var person in people)
                People.Add(person);

            await Task.Delay(TimeSpan.FromSeconds(5), token);
        }
    }

    Task<List<Person>> FetchPeople(CancellationToken cancellationToken)
    {
        var people = Enumerable.Range(1, 100).Select(i => new Person
        {
            Id = i,
            Name = $"Person {i}"
        }).ToList();

        return Task.FromResult(people);
    }
}
```

This code has some problems:
- Every cycle we clear the ObservableCollection, to put back the exact same data
- For each person added the INotifyCollectionChanged.PropertyChanged event is raised, that could cause some serious performance issue on large set
- Every cycle we completely reset the state about selection and we are forced to restore it.

If we want to avoid all unecessary events to be raised we have to: 
- Remove only the old elements
- Add only the new elements
- Reorder the existing ones to be in the correct position
- Restore the selection

It should be easier than that!

# ObservableHashList
Let's replace the ObservableCollection with an instance of ObservableHashList:

```csharp
public class Person
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

internal class MainWindowViewModel : ObservableObject
{
    CancellationTokenSource _cancellationTokenSource = new();
    public MainWindowViewModel() => _ = Poll();
    public ObservableHashList<Person> People { get; }
        = ObservableHashList.New<Person>()
                            .WithSelectionKey(person => person.Id)
                            .ForEqualityCheckAlso(person => new { person.Name })
                            .OnUpdateReplaceItem();

    async Task Poll()
    {
        var token = _cancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            var people = await FetchPeople(token);
            People.Refresh(people);

            await Task.Delay(TimeSpan.FromSeconds(5), token);
        }
    }

    Task<List<Person>> FetchPeople(CancellationToken cancellationToken)
    {
        var people = Enumerable.Range(1, 100).Select(i => new Person
        {
            Id = i,
            Name = $"Person {i}"
        }).ToList();

        return Task.FromResult(people);
    }
}
```

We have replaced the ObservableCollection with an instance of ObservableHashList. We have to specify 3 things:
- When 2 objects with different references are pointing to the "same" conceptual object. This is done through the line 
`.WithSelectionKey(person => person.Id)`. You can use also a different overload and pass an `IEqualityComparer<T>`.
- When 2 objects with maybe different references represent exactly the same object. This is done through the line 
`.ForEqualityCheckAlso(person => new { person.Name })`. You can use also a different overload and pass an `IEqualityComparer<T>`.
- The update strategy

The `Refresh()` method of the ObservableHashList is conceptually a `Clear(); AddRange(people);` call. This is what it does in detail:
- Every old element in the collection that is not contained in the new list is removed. The items are compared using the selection key. So, in the example above, any person with an id that is not contained in the new people list is removed.
- If an item in the new list is already contained in the collection (the check is done using the selection key), if they are NOT equal according to the equality comparer passed, then the old item is updated with the new one. There are 2 built in strategies for updates.
- Every new item is added
- The elements are reordered in the correct position

The events are raised in a smart way. For example if only 20 consecutives elements have to be inserted, the `Refesh()` method raises only once the CollectionChanged event.

Note: We can exploit records:
```csharp
public record Person
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

    public ObservableHashList<Person> People { get; }
        = ObservableHashList.New<Person>()
                            .WithSelectionKey(person => person.Id)
                            .WithDefaultEquality()
                            .OnUpdateReplaceItem();
```

The code above means: Add every person with different Id. If 2 people have the same id, use the default equality comparer to check if they are equal. For records that is equivalent to check the equality for all the fields, and that is ideal fo DTOs for example. If 2 elements have same selection key but are different according to the equality comparer, update the old item with the new one.

# Update strategies:

```csharp
    public ObservableHashList<Person> People { get; }
        = ObservableHashList.New<Person>()
                            .WithSelectionKey(person => person.Id)
                            .WithDefaultEquality()
                            .OnUpdateReplaceItem();
```
`OnUpdateReplaceItem` means that on update (so if 2 elements have same selection key, but are not equal according to the equality comparer), the old element is removed and the newer one is added.

```csharp
    public ObservableHashList<Person> People { get; }
        = ObservableHashList.New<Person>()
                            .WithSelectionKey(person => person.Id)
                            .WithDefaultEquality()
                            .OnUpdateReplaceItem();
```
`OnUpdateDeepCopy` means that all the propertise of the new element are copied into the old one recursively, invoking automatically the PropertyChanged event for every property set. The class must implement the INotifyPropertyChanged for this to work properly.

```csharp
public record Person : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public required int Id { get; init; }
    public required string Name { get; init; }
}
 public ObservableHashList<Person> People { get; }
       = ObservableHashList.New<Person>()
                           .WithSelectionKey(person => person.Id)
                           .WithDefaultEquality()
                           .OnUpdateDeepCopy();
```
