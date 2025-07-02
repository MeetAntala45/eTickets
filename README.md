# 🎬 Movie Rent Management System

**Demo** : https://drive.google.com/file/d/1D7akSxS7IaJLabl-q2CrohESEnm_-EHe/view

## Project Overview
The **Movie Rent Management System** is a web application that allows users to rent movies online. Users can browse through available movies, view details, and place rental orders. Admin users can manage movie data and other related entities, such as production companies, producers, and actors. The system provides an efficient platform for managing movie rentals and organizing movie-related information.


## Features

### User Features
- 📽️ **Browse Movies**: View all available movies with details like name, description, price, and production information.
- 🛒 **Rent Movies**: Users can rent movies and track their orders.
- 📜 **View Orders**: Users can view their rental history of all placed orders.


### Admin Features
- 🛠️ **Manage Movies**: Add, or edit delete movies. When creating a movie, admins can select the production company, producer, and actors from existing entities.
- 🏢 **Manage Production Companies**: Add or edit production companies.
- 🎬 **Manage Producers & Actors**: Add or edit producer and actor details.
- 📊 **View All Orders**: Admins can view all rental orders placed by users.


## Order Management
- Users can place orders to rent movies.
- Admins can view all orders placed by users.
- Each order contains details about the user, the movie rented and total price.


## Tech Stack
- **Backend**: .NET Core MVC (NET 8.0)
- **Database**: Microsoft SQL Server
- **ORM**: Entity Framework Core


## Installation & Setup

### Prerequisites
- **Visual Studio 2022** (with .NET 8.0 SDK installed)
- **SQL Server** (local or cloud-based)
- **NuGet Packages**:
  - `Microsoft.EntityFrameworkCore`
  - `Microsoft.EntityFrameworkCore.SqlServer`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`

### Steps to Run in Visual Studio

1. **Clone the repository**:
   ```bash
   git clone https://github.com/MeetAntala45/moviesRentManagementSystem.git
   
2. Open `.sln` file in Visual Studio.
   
3. Set up the database connection: In the `appsettings.json` file, update the connection string to match your local SQL Server configuration:
    ```bash
    "ConnectionStrings": {
      "DefaultConnectionString": "Server=YOUR_SERVER_NAME;Initial Catalog=rentmoviesdb;Integrated Security=True;Connect Timeout=30;"
    },

4. Run this command in package manager console.
    ```bash
    add-migration initial
    update-database
