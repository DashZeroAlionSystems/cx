using System.Data.Common;
using CX.Engine.Common.Db;
using Moq;

namespace CX.Engine.Common.Tests;

public class DbDataReaderTests
{
    [Fact]
    public void GetNullable_WhenColumnIsNull_ReturnsDefault()
    {
        // Arrange
        var reader = new Mock<DbDataReader>();
        reader.Setup(r => r.GetOrdinal("column")).Returns(0);
        reader.Setup(r => r.IsDBNull(0)).Returns(true);
        
        // Act
        var result = reader.Object.GetNullable<int>("column");
        
        // Assert
        Assert.Equal(default, result);
    }
    
    [Fact]
    public void GetNullable_WhenColumnIsNotNull_ReturnsValue()
    {
        // Arrange
        var reader = new Mock<DbDataReader>();
        reader.Setup(r => r.GetOrdinal("column")).Returns(0);
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.GetFieldValue<int>(0)).Returns(1);
        
        // Act
        var result = reader.Object.GetNullable<int>("column");
        
        // Assert
        Assert.Equal(1, result);
    }
    
    [Fact]
    public void Get_WhenColumnIsNotNull_ReturnsValue()
    {
        // Arrange
        var reader = new Mock<DbDataReader>();
        reader.Setup(r => r.GetOrdinal("column")).Returns(0);
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.GetFieldValue<int>(0)).Returns(1);
        
        // Act
        var result = reader.Object.Get<int>("column");
        
        // Assert
        Assert.Equal(1, result);
    }
    
    [Fact]
    public void GetNullable_WhenOrdinalIsNull_ReturnsDefault()
    {
        // Arrange
        var reader = new Mock<DbDataReader>();
        reader.Setup(r => r.IsDBNull(0)).Returns(true);
        
        // Act
        var result = reader.Object.GetNullable<int>(0);
        
        // Assert
        Assert.Equal(default, result);
    }
    
    [Fact]
    public void GetNullable_WhenOrdinalIsNotNull_ReturnsValue()
    {
        // Arrange
        var reader = new Mock<DbDataReader>();
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.GetFieldValue<int>(0)).Returns(1);
        
        // Act
        var result = reader.Object.GetNullable<int>(0);
        
        // Assert
        Assert.Equal(1, result);
    }
    
   
    [Fact]
    public void Get_WhenOrdinalIsNotNull_ReturnsValue()
    {
        // Arrange
        var reader = new Mock<DbDataReader>();
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.GetFieldValue<int>(0)).Returns(1);
        
        // Act
        var result = reader.Object.Get<int>(0);
        
        // Assert
        Assert.Equal(1, result);
    }
}