# Online Store Frontend

React TypeScript frontend for the OnlineStoreShop e-commerce application.

## 🚀 Features

- **Product Catalog**: Browse products with search and category filtering
- **Product Details**: View detailed information, reviews, and ratings
- **Shopping Cart**: Add items to cart, update quantities, manage cart
- **User Authentication**: Login, register, and profile management
- **Order Management**: View order history and status
- **Responsive Design**: Mobile-friendly interface using Material-UI
- **TypeScript**: Full type safety throughout the application

## 🛠️ Technology Stack

- **React 18** - Frontend framework
- **TypeScript** - Type safety
- **Material-UI (MUI)** - UI component library
- **React Router** - Client-side routing
- **Axios** - HTTP client for API calls
- **React Context** - State management

## 📋 Prerequisites

- Node.js 16+ and npm
- Backend API server running (OnlineStoreShop .NET Core API)

## 🚀 Getting Started

1. **Install dependencies**:
   ```bash
   npm install
   ```

2. **Start the development server**:
   ```bash
   npm start
   ```

3. **Open your browser** and navigate to `http://localhost:3000`

## 🏗️ Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── Header/         # Navigation header
│   └── ProtectedRoute/ # Route protection component
├── contexts/           # React contexts
│   └── AuthContext.tsx # Authentication state management
├── pages/              # Page components
│   ├── Home.tsx        # Landing page
│   ├── Products.tsx    # Product catalog
│   ├── ProductDetail.tsx # Individual product page
│   ├── Cart.tsx        # Shopping cart
│   ├── Login.tsx       # User login
│   ├── Register.tsx    # User registration
│   ├── Profile.tsx     # User profile
│   └── Orders.tsx      # Order history
├── services/           # API services
│   └── api.ts          # API client configuration
├── types/              # TypeScript type definitions
│   └── index.ts        # Shared interfaces
├── App.tsx             # Main app component
└── index.tsx           # App entry point
```

## 🔧 Configuration

### API Configuration

The API base URL is configured in `src/services/api.ts`:

```typescript
const API_BASE_URL = 'http://localhost:5000/api';
```

Update this to match your backend server URL.

### Authentication

The app uses JWT token-based authentication:
- Tokens are stored in localStorage
- Automatic token injection in API requests
- Protected routes with automatic redirects

## 📱 Available Pages

1. **Home** (`/`) - Landing page with featured products
2. **Products** (`/products`) - Product catalog with filtering
3. **Product Detail** (`/products/:id`) - Individual product information
4. **Cart** (`/cart`) - Shopping cart management
5. **Login** (`/login`) - User authentication
6. **Register** (`/register`) - User registration
7. **Profile** (`/profile`) - User profile management (protected)
8. **Orders** (`/orders`) - Order history (protected)

## 🎨 UI Components

The application uses Material-UI components with a custom theme:
- Primary color: Blue (#1976d2)
- Secondary color: Red (#dc004e)
- Responsive design with mobile-first approach

## 🔄 API Integration

The frontend integrates with the following API endpoints:

### Products
- `GET /api/products` - Get products with pagination
- `GET /api/products/:id` - Get product details
- `GET /api/products/featured` - Get featured products
- `POST /api/products/:id/reviews` - Add product review

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `GET /api/auth/me` - Get current user

### Shopping Cart
- `GET /api/shoppingcart` - Get cart contents
- `POST /api/shoppingcart/add` - Add item to cart
- `PUT /api/shoppingcart/items/:id` - Update cart item
- `DELETE /api/shoppingcart/items/:id` - Remove cart item

### Orders
- `GET /api/orders` - Get user orders
- `GET /api/orders/:id` - Get order details
- `POST /api/orders` - Create new order

## 🧪 Build for Production

```bash
npm run build
```

This creates an optimized production build in the `build/` directory.

## 🐛 Troubleshooting

### Common Issues

1. **API Connection Error**:
   - Ensure the backend API server is running
   - Check the API_BASE_URL configuration
   - Verify CORS settings on the backend

2. **Authentication Issues**:
   - Clear browser localStorage if needed
   - Check JWT token expiration
   - Verify API authentication endpoints

3. **Build Errors**:
   - Clear node_modules and reinstall: `rm -rf node_modules package-lock.json && npm install`
   - Check TypeScript configuration

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License.