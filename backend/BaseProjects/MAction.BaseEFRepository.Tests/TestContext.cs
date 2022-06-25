﻿using MAction.BaseEFRepository;
using Microsoft.EntityFrameworkCore;

namespace MAction.BaseEFRepository.Tests;

internal class TestContext : ApplicationContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TestContext(DbContextOptions<TestContext> options) : base(options, typeof(DoctorTest))
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public virtual DbSet<DoctorTest> Doctors { get; set; }

}