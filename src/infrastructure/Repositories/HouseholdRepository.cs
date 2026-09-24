using domain.Models;
using infrastructure.Data;
using infrastructure.Interfaces;
using infrastructure.Repositories.Generics;

namespace infrastructure.Repositories;

public class HouseholdRepository(AppDbContext context) : GenericRepository<Household>(context), IHouseholdRepository;