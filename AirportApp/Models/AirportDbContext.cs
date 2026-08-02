using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AirportApp.Models;

public partial class AirportDbContext : DbContext
{
    public AirportDbContext()
    {
    }

    public AirportDbContext(DbContextOptions<AirportDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Airplane> Airplanes { get; set; }

    public virtual DbSet<AirplanesDatum> AirplanesData { get; set; }

    public virtual DbSet<Airport> Airports { get; set; }

    public virtual DbSet<AirportsDatum> AirportsData { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<BoardingPass> BoardingPasses { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Flight> Flights { get; set; }

    public virtual DbSet<FlightChangeHistory> FlightChangeHistories { get; set; }

    public virtual DbSet<FlightChangeRequest> FlightChangeRequests { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<Segment> Segments { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketFlight> TicketFlights { get; set; }

    public virtual DbSet<Timetable> Timetables { get; set; }

    public virtual DbSet<TransactionHistory> TransactionHistories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            try
            {
                DotNetEnv.Env.Load();
            }
            catch { }
            var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                          ?? "Host=localhost;Port=5432;Database=demo;Username=airportuser;Password=airport123";
            optionsBuilder.UseNpgsql(connStr);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("btree_gist")
            .HasPostgresExtension("cube")
            .HasPostgresExtension("earthdistance");

        modelBuilder.Entity<Airplane>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("airplanes", "bookings");

            entity.Property(e => e.AirplaneCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airplane code, IATA")
                .HasColumnName("airplane_code");
            entity.Property(e => e.Model)
                .HasComment("Airplane model")
                .HasColumnName("model");
            entity.Property(e => e.Range)
                .HasComment("Maximum flight range, km")
                .HasColumnName("range");
            entity.Property(e => e.Speed)
                .HasComment("Cruise speed, km/h")
                .HasColumnName("speed");
        });

        modelBuilder.Entity<AirplanesDatum>(entity =>
        {
            entity.HasKey(e => e.AirplaneCode).HasName("airplanes_data_pkey");

            entity.ToTable("airplanes_data", "bookings", tb => tb.HasComment("Airplanes (internal multilingual data)"));

            entity.Property(e => e.AirplaneCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airplane code, IATA")
                .HasColumnName("airplane_code");
            entity.Property(e => e.Model)
                .HasComment("Airplane model")
                .HasColumnType("jsonb")
                .HasColumnName("model");
            entity.Property(e => e.Range)
                .HasComment("Maximum flight range, km")
                .HasColumnName("range");
            entity.Property(e => e.Speed)
                .HasComment("Cruise speed, km/h")
                .HasColumnName("speed");
        });

        modelBuilder.Entity<Airport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("airports", "bookings");

            entity.Property(e => e.AirportCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airport code, IATA")
                .HasColumnName("airport_code");
            entity.Property(e => e.AirportName)
                .HasComment("Airport name")
                .HasColumnName("airport_name");
            entity.Property(e => e.City)
                .HasComment("City")
                .HasColumnName("city");
            entity.Property(e => e.Coordinates)
                .HasComment("Airport coordinates (longitude and latitude)")
                .HasColumnName("coordinates");
            entity.Property(e => e.Country)
                .HasComment("Country")
                .HasColumnName("country");
            entity.Property(e => e.Timezone)
                .HasComment("Airport time zone")
                .HasColumnName("timezone");
        });

        modelBuilder.Entity<AirportsDatum>(entity =>
        {
            entity.HasKey(e => e.AirportCode).HasName("airports_data_pkey");

            entity.ToTable("airports_data", "bookings", tb => tb.HasComment("Airports (internal multilingual data)"));

            entity.Property(e => e.AirportCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airport code, IATA")
                .HasColumnName("airport_code");
            entity.Property(e => e.AirportName)
                .HasComment("Airport name")
                .HasColumnType("jsonb")
                .HasColumnName("airport_name");
            entity.Property(e => e.City)
                .HasComment("City")
                .HasColumnType("jsonb")
                .HasColumnName("city");
            entity.Property(e => e.Coordinates)
                .HasComment("Airport coordinates (longitude and latitude)")
                .HasColumnName("coordinates");
            entity.Property(e => e.Country)
                .HasComment("Country")
                .HasColumnType("jsonb")
                .HasColumnName("country");
            entity.Property(e => e.Timezone)
                .HasComment("Airport time zone")
                .HasColumnName("timezone");
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<BoardingPass>(entity =>
        {
            entity.HasKey(e => new { e.TicketNo, e.FlightId }).HasName("boarding_passes_pkey");

            entity.ToTable("boarding_passes", "bookings", tb => tb.HasComment("Boarding passes"));

            entity.HasIndex(e => new { e.FlightId, e.BoardingNo }, "boarding_passes_flight_id_boarding_no_key").IsUnique();

            entity.HasIndex(e => new { e.FlightId, e.SeatNo }, "boarding_passes_flight_id_seat_no_key").IsUnique();

            entity.Property(e => e.TicketNo)
                .HasComment("Ticket number")
                .HasColumnName("ticket_no");
            entity.Property(e => e.FlightId)
                .HasComment("Flight ID")
                .HasColumnName("flight_id");
            entity.Property(e => e.BoardingNo)
                .HasComment("Boarding pass number")
                .HasColumnName("boarding_no");
            entity.Property(e => e.BoardingTime)
                .HasComment("Boarding time")
                .HasColumnName("boarding_time");
            entity.Property(e => e.SeatNo)
                .HasComment("Seat number")
                .HasColumnName("seat_no");

            entity.HasOne(d => d.Segment).WithOne(p => p.BoardingPass)
                .HasForeignKey<BoardingPass>(d => new { d.TicketNo, d.FlightId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("boarding_passes_ticket_no_flight_id_fkey");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookRef).HasName("bookings_pkey");

            entity.ToTable("bookings", "bookings", tb => tb.HasComment("Bookings"));

            entity.Property(e => e.BookRef)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasComment("Booking number")
                .HasColumnName("book_ref");
            entity.Property(e => e.BookDate)
                .HasComment("Booking date")
                .HasColumnName("book_date");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2)
                .HasComment("Total booking amount")
                .HasColumnName("total_amount");
        });

        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.FlightId).HasName("flights_pkey");

            entity.ToTable("flights", "bookings", tb => tb.HasComment("Flights"));

            entity.HasIndex(e => new { e.RouteNo, e.ScheduledDeparture }, "flights_route_no_scheduled_departure_key").IsUnique();

            entity.Property(e => e.FlightId)
                .HasComment("Flight ID")
                .UseIdentityAlwaysColumn()
                .HasColumnName("flight_id");
            entity.Property(e => e.ActualArrival)
                .HasComment("Actual arrival time")
                .HasColumnName("actual_arrival");
            entity.Property(e => e.ActualDeparture)
                .HasComment("Actual departure time")
                .HasColumnName("actual_departure");
            entity.Property(e => e.RouteNo)
                .HasComment("Route number")
                .HasColumnName("route_no");
            entity.Property(e => e.ScheduledArrival)
                .HasComment("Scheduled arrival time")
                .HasColumnName("scheduled_arrival");
            entity.Property(e => e.ScheduledDeparture)
                .HasComment("Scheduled departure time")
                .HasColumnName("scheduled_departure");
            entity.Property(e => e.Status)
                .HasComment("Flight status")
                .HasColumnName("status");
        });

        modelBuilder.Entity<FlightChangeHistory>(entity =>
        {
            entity.HasKey(e => e.ChangeId).HasName("flight_change_history_pkey");

            entity.ToTable("flight_change_history", "bookings");

            entity.Property(e => e.ChangeId)
                .HasDefaultValueSql("nextval('flight_change_history_change_id_seq'::regclass)")
                .HasColumnName("change_id");
            entity.Property(e => e.BookRef)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("book_ref");
            entity.Property(e => e.ChangeDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("change_date");
            entity.Property(e => e.NewFlightId).HasColumnName("new_flight_id");
            entity.Property(e => e.OldFlightId).HasColumnName("old_flight_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasColumnName("reason");

            entity.HasOne(d => d.BookRefNavigation).WithMany(p => p.FlightChangeHistories)
                .HasForeignKey(d => d.BookRef)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("flight_change_history_book_ref_fkey");

            entity.HasOne(d => d.NewFlight).WithMany(p => p.FlightChangeHistoryNewFlights)
                .HasForeignKey(d => d.NewFlightId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("flight_change_history_new_flight_id_fkey");

            entity.HasOne(d => d.OldFlight).WithMany(p => p.FlightChangeHistoryOldFlights)
                .HasForeignKey(d => d.OldFlightId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("flight_change_history_old_flight_id_fkey");
        });

        modelBuilder.Entity<FlightChangeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("flight_change_requests_pkey");

            entity.ToTable("flight_change_requests", "bookings");

            entity.Property(e => e.RequestId)
                .HasDefaultValueSql("nextval('flight_change_requests_request_id_seq'::regclass)")
                .HasColumnName("request_id");
            entity.Property(e => e.BookRef)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("book_ref");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_date");
            entity.Property(e => e.RequestedFlightId).HasColumnName("requested_flight_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Pending'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.BookRefNavigation).WithMany(p => p.FlightChangeRequests)
                .HasForeignKey(d => d.BookRef)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("flight_change_requests_book_ref_fkey");

            entity.HasOne(d => d.RequestedFlight).WithMany(p => p.FlightChangeRequests)
                .HasForeignKey(d => d.RequestedFlightId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("flight_change_requests_requested_flight_id_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("orders_pkey");

            entity.ToTable("orders", "bookings");

            entity.Property(e => e.OrderId)
                .HasDefaultValueSql("nextval('orders_order_id_seq'::regclass)")
                .HasColumnName("order_id");
            entity.Property(e => e.BookRef)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("book_ref");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("order_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2)
                .HasColumnName("total_amount");

            entity.HasOne(d => d.BookRefNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.BookRef)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("orders_book_ref_fkey");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("order_details_pkey");

            entity.ToTable("order_details", "bookings");

            entity.Property(e => e.OrderDetailId)
                .HasDefaultValueSql("nextval('order_details_order_detail_id_seq'::regclass)")
                .HasColumnName("order_detail_id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.OrderId).HasColumnName("order_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_details_order_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payments_pkey");

            entity.ToTable("payments", "bookings");

            entity.HasIndex(e => e.ExternalTransactionId, "payments_external_transaction_id_key").IsUnique();

            entity.Property(e => e.PaymentId)
                .HasDefaultValueSql("nextval('payments_payment_id_seq'::regclass)")
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.ConfirmationDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("confirmation_date");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValueSql("'USD'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.ExternalTransactionId)
                .HasMaxLength(100)
                .HasColumnName("external_transaction_id");
            entity.Property(e => e.Gateway)
                .HasMaxLength(50)
                .HasColumnName("gateway");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("payment_date");
            entity.Property(e => e.ResponseMessage).HasColumnName("response_message");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Completed'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payments_order_id_fkey");
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("routes", "bookings", tb => tb.HasComment("Routes"));

            entity.HasIndex(e => new { e.RouteNo, e.Validity }, "routes_route_no_validity_excl").HasMethod("gist");

            entity.Property(e => e.AirplaneCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airplane code, IATA")
                .HasColumnName("airplane_code");
            entity.Property(e => e.ArrivalAirport)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airport of arrival")
                .HasColumnName("arrival_airport");
            entity.Property(e => e.DaysOfWeek)
                .HasComment("Days of week array")
                .HasColumnName("days_of_week");
            entity.Property(e => e.DepartureAirport)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airport of departure")
                .HasColumnName("departure_airport");
            entity.Property(e => e.Duration)
                .HasComment("Estimated duration")
                .HasColumnName("duration");
            entity.Property(e => e.RouteNo)
                .HasComment("Route number")
                .HasColumnName("route_no");
            entity.Property(e => e.ScheduledTime)
                .HasComment("Scheduled local time of departure")
                .HasColumnName("scheduled_time");
            entity.Property(e => e.Validity)
                .HasComment("Period of validity")
                .HasColumnName("validity");

            entity.HasOne(d => d.AirplaneCodeNavigation).WithMany()
                .HasForeignKey(d => d.AirplaneCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("routes_airplane_code_fkey");

            entity.HasOne(d => d.ArrivalAirportNavigation).WithMany()
                .HasForeignKey(d => d.ArrivalAirport)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("routes_arrival_airport_fkey");

            entity.HasOne(d => d.DepartureAirportNavigation).WithMany()
                .HasForeignKey(d => d.DepartureAirport)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("routes_departure_airport_fkey");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => new { e.AirplaneCode, e.SeatNo }).HasName("seats_pkey");

            entity.ToTable("seats", "bookings", tb => tb.HasComment("Seats"));

            entity.Property(e => e.AirplaneCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airplane code, IATA")
                .HasColumnName("airplane_code");
            entity.Property(e => e.SeatNo)
                .HasComment("Seat number")
                .HasColumnName("seat_no");
            entity.Property(e => e.FareConditions)
                .HasComment("Travel class")
                .HasColumnName("fare_conditions");

            entity.HasOne(d => d.AirplaneCodeNavigation).WithMany(p => p.Seats)
                .HasForeignKey(d => d.AirplaneCode)
                .HasConstraintName("seats_airplane_code_fkey");
        });

        modelBuilder.Entity<Segment>(entity =>
        {
            entity.HasKey(e => new { e.TicketNo, e.FlightId }).HasName("segments_pkey");

            entity.ToTable("segments", "bookings", tb => tb.HasComment("Flight segment (leg)"));

            entity.HasIndex(e => e.FlightId, "segments_flight_id_idx");

            entity.Property(e => e.TicketNo)
                .HasComment("Ticket number")
                .HasColumnName("ticket_no");
            entity.Property(e => e.FlightId)
                .HasComment("Flight ID")
                .HasColumnName("flight_id");
            entity.Property(e => e.FareConditions)
                .HasComment("Travel class")
                .HasColumnName("fare_conditions");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasComment("Travel price")
                .HasColumnName("price");

            entity.HasOne(d => d.Flight).WithMany(p => p.Segments)
                .HasForeignKey(d => d.FlightId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("segments_flight_id_fkey");

            entity.HasOne(d => d.TicketNoNavigation).WithMany(p => p.Segments)
                .HasForeignKey(d => d.TicketNo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("segments_ticket_no_fkey");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.TicketNo).HasName("tickets_pkey");

            entity.ToTable("tickets", "bookings", tb => tb.HasComment("Tickets"));

            entity.HasIndex(e => new { e.BookRef, e.PassengerId, e.Outbound }, "tickets_book_ref_passenger_id_outbound_key").IsUnique();

            entity.Property(e => e.TicketNo)
                .HasComment("Ticket number")
                .HasColumnName("ticket_no");
            entity.Property(e => e.BookRef)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasComment("Booking number")
                .HasColumnName("book_ref");
            entity.Property(e => e.Outbound)
                .HasComment("Outbound flight?")
                .HasColumnName("outbound");
            entity.Property(e => e.PassengerId)
                .HasComment("Passenger ID")
                .HasColumnName("passenger_id");
            entity.Property(e => e.PassengerName)
                .HasComment("Passenger name")
                .HasColumnName("passenger_name");

            entity.HasOne(d => d.BookRefNavigation).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.BookRef)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tickets_book_ref_fkey");
        });

        modelBuilder.Entity<TicketFlight>(entity =>
        {
            entity.HasKey(e => new { e.TicketNo, e.FlightId }).HasName("ticket_flights_pkey");

            entity.ToTable("ticket_flights", "bookings");

            entity.Property(e => e.TicketNo)
                .HasMaxLength(13)
                .IsFixedLength()
                .HasColumnName("ticket_no");
            entity.Property(e => e.FlightId).HasColumnName("flight_id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.FareConditions)
                .HasMaxLength(10)
                .HasColumnName("fare_conditions");

            entity.HasOne(d => d.Flight).WithMany(p => p.TicketFlights)
                .HasForeignKey(d => d.FlightId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ticket_flights_flight_id_fkey");

            entity.HasOne(d => d.TicketNoNavigation).WithMany(p => p.TicketFlights)
                .HasForeignKey(d => d.TicketNo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ticket_flights_ticket_no_fkey");
        });

        modelBuilder.Entity<Timetable>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("timetable", "bookings");

            entity.Property(e => e.ActualArrival)
                .HasComment("Actual arrival time")
                .HasColumnName("actual_arrival");
            entity.Property(e => e.ActualArrivalLocal)
                .HasComment("Actual arrival time in airport's timezone")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actual_arrival_local");
            entity.Property(e => e.ActualDeparture)
                .HasComment("Actual departure time")
                .HasColumnName("actual_departure");
            entity.Property(e => e.ActualDepartureLocal)
                .HasComment("Actual departure time in airport's timezone")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actual_departure_local");
            entity.Property(e => e.AirplaneCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airplane code, IATA")
                .HasColumnName("airplane_code");
            entity.Property(e => e.ArrivalAirport)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airport of arrival")
                .HasColumnName("arrival_airport");
            entity.Property(e => e.DepartureAirport)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Airport of departure")
                .HasColumnName("departure_airport");
            entity.Property(e => e.FlightId)
                .HasComment("Flight ID")
                .HasColumnName("flight_id");
            entity.Property(e => e.RouteNo)
                .HasComment("Route number")
                .HasColumnName("route_no");
            entity.Property(e => e.ScheduledArrival)
                .HasComment("Scheduled arrival time")
                .HasColumnName("scheduled_arrival");
            entity.Property(e => e.ScheduledArrivalLocal)
                .HasComment("Scheduled arrival time in airport's timezone")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("scheduled_arrival_local");
            entity.Property(e => e.ScheduledDeparture)
                .HasComment("Scheduled departure time")
                .HasColumnName("scheduled_departure");
            entity.Property(e => e.ScheduledDepartureLocal)
                .HasComment("Scheduled departure time in airport's timezone")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("scheduled_departure_local");
            entity.Property(e => e.Status)
                .HasComment("Flight status")
                .HasColumnName("status");
        });

        modelBuilder.Entity<TransactionHistory>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("transaction_history_pkey");

            entity.ToTable("transaction_history", "bookings");

            entity.Property(e => e.TransactionId)
                .HasDefaultValueSql("nextval('transaction_history_transaction_id_seq'::regclass)")
                .HasColumnName("transaction_id");
            entity.Property(e => e.BookRef)
                .HasMaxLength(6)
                .IsFixedLength()
                .HasColumnName("book_ref");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("transaction_date");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(100)
                .HasColumnName("transaction_type");
            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .HasColumnName("user_id");

            entity.HasOne(d => d.BookRefNavigation).WithMany(p => p.TransactionHistories)
                .HasForeignKey(d => d.BookRef)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transaction_history_book_ref_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
