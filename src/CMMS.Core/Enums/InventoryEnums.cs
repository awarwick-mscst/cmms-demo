namespace CMMS.Core.Enums;

public enum PartStatus
{
    Active,
    Inactive,
    Obsolete,
    Discontinued
}

public enum TransactionType
{
    Receive,
    Issue,
    Adjust,
    Transfer,
    Reserve,
    Unreserve
}

public enum ReorderStatus
{
    Ok,
    Low,
    Critical,
    OutOfStock
}

public enum UnitOfMeasure
{
    Each,
    Foot,
    Meter,
    Gallon,
    Quart,
    Liter,
    Pound,
    Kilogram,
    Box,
    Case,
    Roll,
    Set,
    Pair
}
