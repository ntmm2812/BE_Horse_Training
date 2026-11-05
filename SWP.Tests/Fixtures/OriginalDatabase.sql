GO
CREATE DATABASE Horse_managerment;
GO
USE Horse_managerment;
GO
CREATE TABLE Role
(
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE
);
GO
CREATE TABLE Permission
(
    PermissionID INT IDENTITY(1,1) PRIMARY KEY,
    PermissionName VARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME NOT NULL,
);
GO
CREATE TABLE RolePermission
(
    RolePermissionID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID INT NOT NULL,
    PermissionID INT NOT NULL,
    CreatedAt DATETIME NOT NULL
	FOREIGN KEY (RoleID) REFERENCES Role(RoleID),
	FOREIGN KEY (PermissionID) REFERENCES Permission(PermissionID),

);
GO
CREATE TABLE Users
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PhoneNumber VARCHAR(20) NULL,
    PasswordHash VARCHAR(255) NULL,
    DateOfBirth DATE NULL,
    Gender VARCHAR(20) NULL,
    Address NVARCHAR(255) NULL,
    Status VARCHAR(20) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    GoogleID NVARCHAR(MAX) NULL,
    RoleID INT NOT NULL,
    FOREIGN KEY (RoleID) REFERENCES Role(RoleID),

);
GO
CREATE TABLE Horse
(
    HorseID INT IDENTITY(1,1) PRIMARY KEY,
    HorseName NVARCHAR(100) NOT NULL,
    Breed NVARCHAR(100) NULL,
    Gender VARCHAR(20) NULL,
    DateOfBirth DATE NULL,
    Color NVARCHAR(50) NULL,
    Weight DECIMAL(6,2) NULL,
    Height DECIMAL(5,2) NULL,
    HorseStatus VARCHAR(30) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    LockReason NVARCHAR(500) NULL,
    LockedByUserID INT NULL,
    LockedAt DATETIME NULL,
	ImageURL varchar(500) NULL,
    UserID INT NOT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (LockedByUserID) REFERENCES Users(UserID),
);

GO
CREATE TABLE HorseHealthStatus (
        HorseID INT NOT NULL PRIMARY KEY,
        TrangThai NVARCHAR(30) NOT NULL,
        NgayCapNhat DATETIME NOT NULL,
        CONSTRAINT FK_HorseHealthStatus_Horse FOREIGN KEY (HorseID) REFERENCES Horse(HorseID) ON DELETE CASCADE
);

GO
CREATE TABLE TrainingPlans
(
    PlanID INT IDENTITY(1,1) PRIMARY KEY,
    PlanName NVARCHAR(150) NOT NULL,
    Objective NVARCHAR(500) NULL,
    TrainingType VARCHAR(50) NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    IntensityLevel VARCHAR(20) NULL,
    PlanStatus VARCHAR(30) NOT NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    HorseID INT NOT NULL,
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID),
);
GO
CREATE TABLE TrainingSchedule
(
    ScheduleID INT IDENTITY(1,1) PRIMARY KEY,
    SessionTitle NVARCHAR(150) NULL,
    TrainingDate DATETIME NOT NULL,
    StartTime DATETIME NULL,
    EndTime DATETIME NULL,
    TrainingType VARCHAR(50) NULL,
    DurationMinutes INT NULL,
    DistanceKm DECIMAL(8,2) NULL,
    PerformanceScore DECIMAL(5,2) NULL,
    Result NVARCHAR(500) NULL,
    Status VARCHAR(30) NOT NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME NOT NULL,
    TrialVideoUrl NVARCHAR(500) NULL,
    PlanID INT NOT NULL,
    AssignedToUserID INT NULL,
    FOREIGN KEY (PlanID) REFERENCES TrainingPlans(PlanID),
    FOREIGN KEY (AssignedToUserID)  REFERENCES Users(UserID),
);
GO


CREATE TABLE HealthMetric
(
    MetricID BIGINT IDENTITY(1,1) PRIMARY KEY,
    HorseID INT NOT NULL,
    HeartRate DECIMAL(6,2) NULL,
    RespiratoryRate DECIMAL(6,2) NULL,
    BodyTemperature DECIMAL(4,2) NULL,
    BloodOxygen DECIMAL(5,2) NULL,
    SpeedKmh DECIMAL(6,2) NULL,
    DistanceKm DECIMAL(8,2) NULL,
    StressLevel VARCHAR(20) NULL,
    RecordedAt DATETIME NOT NULL,
    DataSource VARCHAR(50) NULL,
    Notes NVARCHAR(500) NULL,
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID),

);
GO
CREATE TABLE MedicalRecord
(
    MedicalRecordID INT IDENTITY(1,1) PRIMARY KEY,
    RecordType VARCHAR(50) NULL,
    ViTriChanThuong NVARCHAR(255) NULL,
    Diagnosis NVARCHAR(500) NULL,
    Symptoms NVARCHAR(1000) NULL,
    Treatment NVARCHAR(1000) NULL,
    Medication NVARCHAR(500) NULL,
    Dosage NVARCHAR(200) NULL,
    RecordDate DATETIME NOT NULL,
    FollowUpDate DATE NULL,
    Status VARCHAR(30) NULL,
    Cost DECIMAL(18,2) NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME NOT NULL,
    HorseID INT NOT NULL,
    CreateBy INT NULL,
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID),
    FOREIGN KEY (CreateBy) REFERENCES Users(UserID)
);
GO
CREATE TABLE TrainingLock
(
    LockID INT IDENTITY(1,1) PRIMARY KEY,
    HorseID INT NOT NULL,
    VetID INT NOT NULL,
    LyDo NVARCHAR(200) NOT NULL,
    NgayDat DATE NOT NULL,
    NgayGo DATE NULL,
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID) ON DELETE CASCADE,
    FOREIGN KEY (VetID) REFERENCES Users(UserID)
);
GO
CREATE TABLE VaccinationSchedule
(
    ScheduleID INT IDENTITY(1,1) PRIMARY KEY,
    HorseID INT NOT NULL,
    Loai NVARCHAR(50) NOT NULL,
    NgayHen DATE NOT NULL,
    TrangThai NVARCHAR(30) NOT NULL,
    GhiChu NVARCHAR(500) NULL,
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID) ON DELETE CASCADE
);
GO

CREATE TABLE Stable
(
    StableID INT IDENTITY(1,1) PRIMARY KEY,
    StableName NVARCHAR(100) NOT NULL,
    Location NVARCHAR(255) NULL,
    Capacity INT NULL,
    CurrentOccupancy INT NOT NULL,
    StableType VARCHAR(50) NULL,
    AttachmentUrl NVARCHAR(500) NULL,
    Status VARCHAR(30) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
);
GO
CREATE TABLE StableAssignment
(
    AssignmentID INT IDENTITY(1,1) PRIMARY KEY,
    HorseID INT NOT NULL,
    StableID INT NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NULL,
    Status VARCHAR(30) NOT NULL,
    Notes NVARCHAR(500) NULL,
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID),
    FOREIGN KEY (StableID) REFERENCES Stable(StableID),
);
GO
CREATE TABLE CareTask
(
    CareTaskID INT IDENTITY(1,1) PRIMARY KEY,
    HorseID INT NOT NULL,
    TaskName NVARCHAR(150) NOT NULL,
    TaskType VARCHAR(50) NULL,
    Description NVARCHAR(1000) NULL,
    ScheduledAt DATETIME NULL,
    CompletedAt DATETIME NULL,
    Priority VARCHAR(20) NULL,
    Status VARCHAR(30) NOT NULL,
    CompletionNotes NVARCHAR(1000) NULL,
    UserID INT NULL,
    CreatedAt DATETIME NOT NULL,
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
);
GO
CREATE TABLE FeedingPlan
(
    FeedingPlanID INT IDENTITY(1,1) PRIMARY KEY,
    HorseID INT NOT NULL,
    UserID INT NOT NULL,
    PlanName NVARCHAR(150) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    DailyMealCount INT NULL,
    Status VARCHAR(30) NOT NULL,
    SpecialInstructions NVARCHAR(1000) NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
);
GO
CREATE TABLE FeedingPlanDetail
(
    FeedingPlanDetailID INT IDENTITY(1,1) PRIMARY KEY,
    FeedingPlanID INT NOT NULL,
    MealName NVARCHAR(100) NULL,
    FeedName NVARCHAR(150) NULL,
    Quantity DECIMAL(10,2) NOT NULL,
    Unit VARCHAR(20) NULL,
    FeedingTime TIME NULL,
    Calories DECIMAL(10,2) NULL,
    Instructions NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL,
    FOREIGN KEY (FeedingPlanID) REFERENCES FeedingPlan(FeedingPlanID),
);
GO
CREATE TABLE Supply
(
    SupplyID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NULL,
    SupplyName NVARCHAR(150) NOT NULL,
    Category VARCHAR(50) NULL,
    Description NVARCHAR(500) NULL,
    QuantityInStock DECIMAL(12,2) NOT NULL,
    Unit VARCHAR(20) NULL,
    UnitPrice DECIMAL(18,2) NULL,
    ExpiryDate DATE NULL,
    StorageLocation NVARCHAR(100) NULL,
    Status VARCHAR(30) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
);
GO
CREATE TABLE Race
(
    RaceID INT IDENTITY(1,1) PRIMARY KEY,
    RaceName NVARCHAR(150) NOT NULL,
    RaceCode VARCHAR(50) NULL UNIQUE,
    RaceType VARCHAR(50) NULL,
    Location NVARCHAR(255) NULL,
    RaceDate DATETIME NULL,
    RegistrationDeadline DATETIME NULL,
    DistanceMeters INT NULL,
    MaxParticipants INT NULL,
    EntryFee DECIMAL(18,2) NULL,
    PrizePool DECIMAL(18,2) NULL,
    Status VARCHAR(30) NOT NULL,
    Description NVARCHAR(1000) NULL,
    UserID INT NULL,
	CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
);
GO
CREATE TABLE RaceRegistration
(
    RegistrationID INT IDENTITY(1,1) PRIMARY KEY,
    RaceID INT NOT NULL,
    HorseID INT NOT NULL,
    UserID INT NULL,
    RegistrationDate DATETIME NOT NULL,
    RegistrationStatus VARCHAR(30) NOT NULL,
    HorseNumber INT NULL,
    FinishPosition INT NULL,
    FinishTimeSeconds DECIMAL(10,3) NULL,
    PrizeMoney DECIMAL(18,2) NULL,
    ResultStatus VARCHAR(30) NULL,
    Notes NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (RaceID) REFERENCES Race(RaceID),
    FOREIGN KEY (HorseID) REFERENCES Horse(HorseID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
);
GO
CREATE TABLE FinancialTransaction
(
    TransactionID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    TransactionCode VARCHAR(50) NOT NULL UNIQUE,
    TransactionType VARCHAR(30) NOT NULL,
    Category VARCHAR(50) NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Currency VARCHAR(10) NOT NULL,
    [Date] DATETIME NOT NULL,
    PaymentMethod VARCHAR(30) NULL,
    Status VARCHAR(30) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
);
GO
CREATE TABLE AuditLog
(
    LogID BIGINT IDENTITY(1,1) PRIMARY KEY,
    Action VARCHAR(50) NOT NULL,
    TableName VARCHAR(50) NOT NULL,
    RecordID INT NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    IPAddress VARCHAR(45) NULL,
    CreatedAt DATETIME NOT NULL,
    UserID INT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
);
GO
