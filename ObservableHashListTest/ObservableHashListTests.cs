using FluentAssertions;
using ObservableHashCollections;
using System.Collections;
using System.ComponentModel;

namespace ObservableHashListTest;

public class ObservableHashListTests
{
    [Test]
    public void Should_BeInstantiatedWithNoErrors()
    {
        void InstantiateNew<T>() where T : notnull
        {
            _ = new DeepCopyObservableHashList<T>();
            _ = new DeepCopyObservableHashList<T>(EqualityComparer<T>.Default);
            _ = new DeepCopyObservableHashList<T>(fullEqualityComparer: EqualityComparer<T>.Default);
        }

        InstantiateNew<Room>();
        InstantiateNew<string>();
        InstantiateNew<int>();
    }

    [Test]
    public void InsertAt_Should_InsertInCorrectPositionAnElement()
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();
        var firstToAdd = rooms.Last();
        var secondToAdd = rooms.First();
        var thirdToAdd = rooms.ElementAt(1);
        var fourthRoom = rooms.ElementAt(2);

        collection.Add(firstToAdd);
        collection.Add(secondToAdd);
        collection.Insert(1, thirdToAdd);
        collection.Insert(collection.Count, fourthRoom);

        collection[0].Should().Be(firstToAdd);
        collection[1].Should().Be(thirdToAdd);
        collection[2].Should().Be(secondToAdd);
        collection[3].Should().Be(fourthRoom);

        collection.IndexOf(firstToAdd).Should().Be(0);
        collection.IndexOf(thirdToAdd).Should().Be(1);
        collection.IndexOf(secondToAdd).Should().Be(2);
    }

    [Test]
    public void Clear_AddRange_ShouldWork()
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();
        collection.AddRange(rooms);
        collection.Clear();
        collection.AddRange(rooms);
    }

    [Test]
    public void InsertAt_Should_InsertInCorrectPositionAnElementAsIList()
    {
        IList collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();
        var firstToAdd = rooms.Last();
        var secondToAdd = rooms.First();
        var thirdToAdd = rooms.ElementAt(1);

        collection.Add(firstToAdd);
        collection.Add(secondToAdd);
        collection.Insert(1, thirdToAdd);

        collection[0].Should().Be(firstToAdd);
        collection[1].Should().Be(thirdToAdd);
        collection[2].Should().Be(secondToAdd);

        collection.IndexOf(firstToAdd).Should().Be(0);
        collection.IndexOf(thirdToAdd).Should().Be(1);
        collection.IndexOf(secondToAdd).Should().Be(2);
    }



    [Test]
    public void InsertAt_Should_ThrowIfInsertingAnAlreadyExitentElement()
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();
        var firstToAdd = rooms.Last();
        var secondToAdd = rooms.First();
        var thirdToAdd = rooms.ElementAt(1);

        collection.Add(firstToAdd);
        collection.Add(secondToAdd);
        collection.Insert(1, thirdToAdd);
        var throwingAction = () => collection.Insert(0, thirdToAdd);
        throwingAction.Should().Throw<ArgumentException>();
    }


    [Test]
    public void AddOrUpdate_ShouldAddOrUpdateElement()
    {
        var collection = NewRoomObservableHashCollection();
        var room = NewRooms().First();
        collection.AddOrUpdate(room);
        var containedRoom = collection.First(x => x.Name == room.Name);
        containedRoom.Should().Be(room);
        room = room with { Tag = "NewTagggg" };
        collection.AddOrUpdate(room);
        containedRoom = collection.First(x => x.Name == room.Name);
        containedRoom.Should().Be(room);
        containedRoom.Tag.Should().Be("NewTagggg");
    }

    [Test]
    public void Add_Should_AddElements()
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        List<Room> addedRooms = new();
        foreach (var room in rooms)
        {
            addedRooms.Add(room);
            collection.Add(room);
            addedRooms.SequenceEqual(collection).Should().BeTrue();
            collection.Count.Should().Be(addedRooms.Count);
        }
    }

    [Test]
    public void Add_Should_ThrowIdAddingAnItemWithAlreadyExistentSelectionKey()
    {
        var collection = NewRoomObservableHashCollection();
        var room = new Room { Name = "name" };

        collection.Add(room);
        var copiedRoom = room with { Tag = "Tag", Size = 1 };

        var add = () => collection.Add(copiedRoom);
        add.Should().Throw<ArgumentException>();
    }


    [Test]
    public void Update_Should_UpdateAllProperties()
    {
        var collection = NewRoomObservableHashCollection(false);

        var room = new Room { Name = "room", Size = 1 };
        collection.Add(room);
        var copiedRoom = room with { Size = 2, Tag = "Tag" };
        collection.Update(copiedRoom);
        room.Size.Should().Be(copiedRoom.Size);
        room.Tag.Should().Be(copiedRoom.Tag);
    }

    [Test]
    public void Update_Should_ThrowIfUpdatingANonExistentItem()
    {
        var collection = NewRoomObservableHashCollection();
        var room = new Room { Name = "A" };
        collection.Add(room);

        var update = () => collection.Update(room with { Name = "B" });
        update.Should().Throw<ArgumentException>();
    }


    [Test]
    public void AddRange_Should_AddEveryItem()
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.AddRange(rooms);
        rooms.SequenceEqual(collection).Should().BeTrue();
        collection.Count.Should().Be(rooms.Count);
    }


    [Test]
    [TestCase(0)]
    [TestCase(300)]
    [TestCase(200)]
    public void Remove_Should_RemoveElementsWithCorrectOrder(int randomSeed)
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.AddRange(rooms);
        var random = new Random(randomSeed);
        var shurffledRooms = rooms.OrderBy(_ => random.Next()).ToList();

        foreach (var room in shurffledRooms)
        {
            rooms.Remove(room);
            collection.Remove(room);
            collection.SequenceEqual(rooms).Should().BeTrue();
            collection.Count.Should().Be(rooms.Count);
        }
    }


    [Test]
    public void Contain_Should_ReturnTrueIfItemWithSameSelectionKeyIsContained()
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.AddRange(rooms);
        var roomsCopied = rooms.Select(room => room with { Size = room.Size + 1 });
        foreach (var room in roomsCopied)
            collection.Should().Contain(room);

        var roomsWithDifferentSelectionKey = rooms.Select(room => room with { Name = $"{room.Name}_copy" });
        foreach (var room in roomsWithDifferentSelectionKey)
            collection.Should().NotContain(room);
    }

    [Test]
    public void Clear_Should_RemoveAllItemsFromCollection()
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.AddRange(rooms);
        collection.Clear();

        collection.Should().BeEmpty();
        collection.Count.Should().Be(0);
    }

    [Test]
    public void Refresh_Should_AddRangeIfEmpty()
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.Refresh(rooms);

        collection.SequenceEqual(rooms).Should().BeTrue();
    }

    [Test]
    [TestCase(0)]
    [TestCase(440)]
    [TestCase(20)]
    public void Refresh_ShouldRemoveItemsInCorrectPosition(int randomSeed)
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.Refresh(rooms);

        var random = new Random(randomSeed);

        for (var i = 0; i < rooms.Count; i++)
        {
            var index = random.Next(0, rooms.Count);
            rooms.RemoveAt(index);
            collection.Refresh(rooms);

            collection.SequenceEqual(rooms).Should().BeTrue();
        }
    }


    [Test]
    [TestCase(0)]
    [TestCase(440)]
    [TestCase(20)]
    public void Refresh_ShouldAddItemsInCorrectPosition(int randomSeed)
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.Refresh(rooms);

        var random = new Random(randomSeed);

        var count = rooms.Count;
        for (var i = 0; i < count; i++)
        {
            var index = random.Next(0, rooms.Count);
            var room = new Room { Name = $"{rooms.Count + 1}" };
            rooms.Insert(index, room);
            collection.Refresh(rooms);

            collection.SequenceEqual(rooms).Should().BeTrue();
        }
    }


    [Test]
    public void Refresh_Should_DoNothingIfHandlingElementsWithSameEqualityComparer()
    {
        var collection = ObservableHashList.New<Room>()
                                           .WithSelectionKey(x => x.Name!)
                                           .ForEqualityCheckAlso(x => x.Size)
                                           .OnUpdateDeepCopy();

        var rooms = NewRooms();
        collection.Refresh(rooms);

        var roomsWithDifferentTag = rooms.Select(room => room with { Tag = $"{room.Tag}_copy" }).ToList();
        collection.Refresh(roomsWithDifferentTag);

        collection.SequenceEqual(rooms).Should().BeTrue();
        collection.SequenceEqual(roomsWithDifferentTag).Should().BeFalse();
    }


    [Test]
    [TestCase(0)]
    [TestCase(500)]
    [TestCase(20)]
    public void Refresh_Should_ReorderCorrectlyIfOrderIsChanged(int randomSeed)
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.AddRange(rooms);
        collection.SequenceEqual(rooms).Should().BeTrue();

        for (var i = 0; i < 100; i++)
        {
            var random = new Random(randomSeed + i);
            var shuffled = rooms.OrderBy(_ => random.Next()).ToList();
            collection.Refresh(shuffled);
            collection.SequenceEqual(shuffled).Should().BeTrue();            
        }
    }


    [Test]
    [TestCase(0)]
    [TestCase(444)]
    [TestCase(20)]
    public void Refresh_Should_AddRemoveAndReorderAtSameTime(int randomSeed)
    {
        var collection = NewRoomObservableHashCollection();
        var rooms = NewRooms();

        collection.Refresh(rooms);

        var random = new Random(randomSeed);

        var toAdd = rooms.Count / 2;
        for (var i = 0; i < toAdd; i++)
        {
            var index = random.Next(0, rooms.Count - 1);
            var newRoom = new Room { Name = $"{rooms.Count + 1}" };
            rooms.Insert(index, newRoom);
        }

        var toRemove = rooms.Count / 2;
        for (var i = 0; i < toRemove; i++)
        {
            var index = random.Next(0, rooms.Count - 1);
            rooms.RemoveAt(index);
        }

        var shuffledRooms = rooms.OrderBy(_ => random.Next()).ToList();

        collection.SequenceEqual(shuffledRooms).Should().BeFalse();

        collection.Refresh(shuffledRooms);

        collection.SequenceEqual(shuffledRooms).Should().BeTrue();
    }


    [Test]
    public void Refresh_Should_DeepCopyAllPropertiesIfSameSelectionKeyButDifferentEquality()
    {
        var collection = NewHouseObservableHashCollection(false);
        var houses = NewHouses();

        collection.Refresh(houses);
        var sameHousesDifferentMainRooms = houses.Select(house => house with
        {
            MainRoom = house.MainRoom! with { Name = $"{house.MainRoom.Name}_copy" }
        }).ToList();

        collection.Refresh(sameHousesDifferentMainRooms);

        collection.SequenceEqual(houses, ReferenceEqualityComparer.Instance).Should().BeTrue();
        collection.SequenceEqual(sameHousesDifferentMainRooms).Should().BeTrue();
        collection.SequenceEqual(sameHousesDifferentMainRooms, ReferenceEqualityComparer.Instance).Should().BeFalse();
    }


    [Test]
    public void Refresh_Should_NotifyPropertyChangedOnDeepCopy()
    {
        var collection = NewHouseObservableHashCollection(false);
        var houses = NewHouses().Take(1).ToList();

        collection.Refresh(houses);
        const string houseName = "NewName";
        var sameHousesDifferentNames = houses.Select(house => house with
        {
            MainRoom = house.MainRoom! with { Name = houseName }
        }).ToList();

        var changedRooms = new List<Room>();

        foreach (var house in collection)
        {
            house.MainRoom!.PropertyChanged += (sender, e) =>
            {
                sender.Should().BeOfType<Room>();
                if (e.PropertyName is nameof(Room.Name))
                    changedRooms.Add((Room)sender!);
            };
        }

        collection.Refresh(sameHousesDifferentNames);

        var expectedChangedRooms = collection.Select(x => x.MainRoom).ToList();
        changedRooms.SequenceEqual(expectedChangedRooms).Should().BeTrue();
    }


    [Test]
    public void Refresh_Should_CallRefreshOfNestedObservableCollectionOnChanged()
    {
        var collection = NewHouseObservableHashCollection();
        var houses = NewHouses();
        var rooms = NewRooms();
        var housesWithRooms = houses.Select(house =>
        {
            var newHouse = house with { };
            newHouse.Rooms.Refresh(rooms);
            return newHouse;
        }).ToList();

        collection.Refresh(housesWithRooms);
        foreach (var (collectionHouse, houseWithRooms) in collection.Zip(housesWithRooms))
        {
            collectionHouse.Rooms.Should().NotBeEmpty();
            collectionHouse.Rooms.SequenceEqual(houseWithRooms.Rooms).Should().BeTrue();
        }
    }


    List<House> NewHouses()
    {
        return Enumerable.Range(1, 100).Select(i => new House
        {
            Address = $"{i}",
            MainRoom = new Room { Name = $"{i}" }
        }).ToList();
    }

    List<Room> NewRooms()
    {
        return Enumerable.Range(1, 1000).Select(i => new Room
        {
            Name = $"{i}",
        }).ToList();
    }


    ObservableHashList<House> NewHouseObservableHashCollection(bool onUpdateReplaceItem = true)
    {
        var builder = ObservableHashList.New<House>()
                                        .WithSelectionKey(x => x.Address)
                                        .WithDefaultEquality();

        return onUpdateReplaceItem ? builder.OnUpdateReplaceItem() : builder.OnUpdateDeepCopy();
    }

    ObservableHashList<Room> NewRoomObservableHashCollection(bool onUpdateReplaceItem = true)
    {
        var builder = ObservableHashList.New<Room>()
                                       .WithSelectionKey(x => x.Name!)
                                       .WithDefaultEquality();

        return onUpdateReplaceItem ? builder.OnUpdateReplaceItem() : builder.OnUpdateDeepCopy();
    }



    record House : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string? Address { get; set; }
        public Room? MainRoom { get; set; }
        public ObservableHashList<Room> Rooms { get; }
            = ObservableHashList.New<Room>()
                                .WithSelectionKey(x => x.Name!)
                                .WithDefaultEquality()
                                .OnUpdateDeepCopy();

    }

    record Room : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string? Name { get; set; }
        public int Size { get; set; }
        public string? Tag { get; set; }
    }
}