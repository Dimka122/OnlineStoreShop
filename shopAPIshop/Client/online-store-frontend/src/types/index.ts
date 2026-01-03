export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  salePrice?: number;
  stockQuantity: number;
  imageUrl?: string;
  isActive: boolean;
  isFeatured: boolean;
  createdAt: string;
  updatedAt?: string;
  category: Category;
  reviews: ProductReview[];
  averageRating: number;
}

export interface Category {
  id: number;
  name: string;
  description?: string;
}

export interface ProductReview {
  id: number;
  rating: number;
  comment: string;
  createdAt: string;
  user: {
    id: string;
    firstName: string;
    lastName: string;
  };
}

export interface CartItem {
  id: number;
  product: Product;
  quantity: number;
  price: number;
}

export interface ShoppingCart {
  id: number;
  items: CartItem[];
  totalAmount: number;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role?: 'admin' | 'user';
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface Order {
  id: number;
  orderDate: string;
  totalAmount: number;
  status: string;
  items: OrderItem[];
  shippingAddress: string;
}

export interface OrderItem {
  id: number;
  product: Product;
  quantity: number;
  price: number;
}