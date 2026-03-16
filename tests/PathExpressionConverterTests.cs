using System.Collections.ObjectModel;

namespace Minimal.Behaviors.Wpf.Tests;

public class ChildModel
{
    public string? Name { get; set; }
    public string[]? Tags { get; set; }
    public List<int>? Scores { get; set; }
    public ObservableCollection<ItemModel>? Items { get; set; }
}

public class ItemModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
}

public class RootModel
{
    public int Id { get; init; }
    public string? Title { get; set; }
    public ChildModel? Child { get; set; }
    public ItemModel[]? ItemArray { get; set; }
    public List<ChildModel>? ChildrenList { get; set; }

    private string Secret { get; set; } = "hidden";

    public string? PublicField;
}

[TestFixture]
public class PathExpressionConverterTests
{
    private readonly PathExpressionConverter _converter = new();
    private RootModel _testModel = null!;

    [SetUp]
    public void Setup()
    {
        _testModel = new RootModel
        {
            Id = 1,
            Title = "Test Root",
            Child = new ChildModel
            {
                Name = "Test Child",
                Tags = ["tag1", "tag2", "tag3"],
                Scores = [10, 20, 30],
                Items =
                [
                    new ItemModel { Id = 100, Title = "First Item" },
                    new ItemModel { Id = 200, Title = "Second Item" }
                ]
            },
            ItemArray =
            [
                new ItemModel { Id = 500, Title = "Array Item 1" },
                new ItemModel { Id = 600, Title = "Array Item 2" }
            ],
            ChildrenList =
            [
                new ChildModel { Name = "List Child 1" },
                new ChildModel { Name = "List Child 2" }
            ],
            PublicField = "field value"
        };
    }

    [Test]
    public void Convert_ReturnsNull_WhenSourceIsNull()
    {
        // Arrange
        string path = "Child.Name";

        // Act
        var result = _converter.Convert(null, path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Convert_ReturnsNull_WhenPathIsNullOrEmpty()
    {
        using (Assert.EnterMultipleScope())
        {
            // Arrange & Act & Assert
            Assert.That(_converter.Convert(_testModel, null!), Is.Null);
            Assert.That(_converter.Convert(_testModel, ""), Is.Null);
            Assert.That(_converter.Convert(_testModel, "   "), Is.Null);
        }
    }

    [Test]
    public void Convert_ReturnsPropertyValue_SingleLevel()
    {
        // Arrange
        string path = "Title";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("Test Root"));
    }

    [Test]
    public void Convert_ReturnsPropertyValue_TwoLevels()
    {
        // Arrange
        string path = "Child.Name";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("Test Child"));
    }

    [Test]
    public void Convert_ReturnsPropertyValue_ThreeLevels()
    {
        // Arrange
        string path = "Child.Items[0].Title";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("First Item"));
    }

    [Test]
    public void Convert_ReturnsNull_WhenIntermediatePropertyIsNull()
    {
        // Arrange
        _testModel.Child = null;
        string path = "Child.Name";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Convert_IgnoresPrivateProperties()
    {
        // Arrange
        string path = "Secret";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Convert_IgnoresPublicFields()
    {
        // Arrange
        string path = "PublicField";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Convert_ReturnsArrayElement_ByIndex()
    {
        // Arrange
        string path = "ItemArray[1].Title";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("Array Item 2"));
    }

    [Test]
    public void Convert_ReturnsListElement_ByIndex()
    {
        // Arrange
        string path = "ChildrenList[0].Name";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("List Child 1"));
    }

    [Test]
    public void Convert_ReturnsArrayElementFromNestedProperty()
    {
        // Arrange
        string path = "Child.Tags[2]";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("tag3"));
    }

    [Test]
    public void Convert_ReturnsListElementFromNestedProperty()
    {
        // Arrange
        string path = "Child.Scores[1]";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void Convert_ReturnsObservableCollectionElement_ByIndex()
    {
        // Arrange
        string path = "Child.Items[1].Id";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo(200));
    }

    [Test]
    public void Convert_ReturnsNull_WhenIndexOutOfBounds()
    {
        // Arrange
        string[] testCases =
        [
            "ItemArray[5]",
    "Child.Tags[10]",
    "ChildrenList[-1]",
    "Child.Items[100].Title"
        ];

        foreach (var path in testCases)
        {
            // Act
            var result = _converter.Convert(_testModel, path);

            // Assert
            Assert.That(result, Is.Null, $"Path: {path}");
        }
    }

    [Test]
    public void Convert_ReturnsNull_WhenIndexOnNonIndexableObject()
    {
        // Arrange
        string path = "Child.Name[0]";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Convert_HandlesMultipleIndexersInPath()
    {
        // Arrange
        _testModel.ChildrenList =
[
    new ChildModel
    {
        Name = "First",
        Items =
        [
            new ItemModel { Id = 1, Title = "A" },
            new ItemModel { Id = 2, Title = "B" }
        ]
    },
    new ChildModel
    {
        Name = "Second",
        Items =
        [
            new ItemModel { Id = 3, Title = "C" },
            new ItemModel { Id = 4, Title = "D" }
        ]
    }
];

        string path = "ChildrenList[1].Items[0].Title";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("C"));
    }

    [Test]
    public void Convert_ParsesPathsWithSpacesCorrectly()
    {
        // Arrange
        string path = "  Child  .  Name  ";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("Test Child"));
    }

    [Test]
    public void Convert_HandlesEmptyPathSegments()
    {
        // Arrange
        string path = "Child..Name";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.EqualTo("Test Child"));
    }

    [Test]
    public void Convert_ReturnsNull_ForNonExistentProperty()
    {
        // Arrange
        string path = "NonExistentProperty";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Convert_ReturnsNull_ForNonExistentNestedProperty()
    {
        // Arrange
        string path = "Child.NonExistent.Another";

        // Act
        var result = _converter.Convert(_testModel, path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Convert_HandlesMalformedIndexer()
    {
        // Arrange
        string[] malformedPaths =
        [
            "Child.Tags[",
    "Child.Tags]",
    "Child.Tags[abc]",
    "Child.Tags[1",
    "Child.Tags[]",
    "Child.Tags[-]",
    "Child.Tags[1.5]"
        ];

        foreach (var path in malformedPaths)
        {
            // Act
            var result = _converter.Convert(_testModel, path);

            // Assert
            Assert.That(result, Is.Null, $"Path: {path}");
        }
    }

    [Test]
    public void TryGetValueByPath_ReturnsTrue_WhenValueFound()
    {
        // Arrange
        string path = "Child.Name";

        // Act
        bool success = PathExpressionConverter.TryGetValueByPath(_testModel, path, out var value);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo("Test Child"));
        }
    }

    [Test]
    public void TryGetValueByPath_ReturnsFalse_WhenSourceIsNull()
    {
        // Arrange
        string path = "Child.Name";

        // Act
        bool success = PathExpressionConverter.TryGetValueByPath(null, path, out var value);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(value, Is.Null);
        }
    }

    [Test]
    public void TryGetValueByPath_ReturnsFalse_WhenPathIsEmpty()
    {
        // Arrange & Act
        bool success = PathExpressionConverter.TryGetValueByPath(_testModel, "", out var value);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(value, Is.Null);
        }
    }

    [Test]
    [Repeat(100)]
    public void Convert_UsesTokenCache_ForRepeatedPaths()
    {
        // Arrange
        string path = "Child.Items[0].Title";
        var expected = "First Item";

        // Act
        var result1 = _converter.Convert(_testModel, path);
        var result2 = _converter.Convert(_testModel, path);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result1, Is.EqualTo(expected));
            Assert.That(result2, Is.EqualTo(expected));
        }
    }

}
