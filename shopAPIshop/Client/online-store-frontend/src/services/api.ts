import axios from 'axios';
import { Product, Category, CartItem, ShoppingCart, LoginRequest, RegisterRequest, AuthResponse, Order } from '../types';

const API_BASE_URL = 'https://localhost:54131/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add token to requests if available
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const productsApi = {
  getProducts: async (page = 1, pageSize = 12, categoryId?: number, search?: string) => {
    const params: any = { page, pageSize };
    if (categoryId) params.categoryId = categoryId;
    if (search) params.search = search;
    
    const response = await api.get('/products', { params });
    return response.data;
  },
  
  getProduct: async (id: number): Promise<Product> => {
    const response = await api.get(`/products/${id}`);
    return response.data;
  },
  
  getFeaturedProducts: async (count = 4): Promise<Product[]> => {
    const params: any = { page: 1, pageSize: count, isFeatured: true };
    const response = await api.get('/products', { params });
    // Normalize different response shapes
    const data = response.data;
    if (Array.isArray(data)) return data as Product[];
    if (Array.isArray(data?.products)) return data.products as Product[];
    if (Array.isArray(data?.data)) return data.data as Product[];
    return [];
  },
  
  createReview: async (productId: number, review: { rating: number; comment: string }) => {
    const response = await api.post(`/products/${productId}/reviews`, review);
    return response.data;
  }
  ,
  // Admin: create / update / delete products (requires auth)
  createProduct: async (product: Partial<Product>) => {
    const response = await api.post('/products', product);
    return response.data as Product;
  },

  updateProduct: async (id: number, product: Partial<Product>) => {
    const response = await api.put(`/products/${id}`, product);
    return response.data as Product;
  },

  deleteProduct: async (id: number) => {
    const response = await api.delete(`/products/${id}`);
    return response.data;
  }
  ,
  // Create/Update with image via FormData
  createProductWithImage: async (formData: FormData) => {
    const response = await api.post('/products', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data as Product;
  },

  updateProductWithImage: async (id: number, formData: FormData) => {
    const response = await api.put(`/products/${id}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data as Product;
  }
};

export const categoriesApi = {
  getCategories: async (): Promise<Category[]> => {
    const response = await api.get('/categories');
    return response.data;
  }
};

export const authApi = {
  login: async (credentials: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post('/auth/login', credentials);
    return response.data;
  },
  
  register: async (userData: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post('/auth/register', userData);
    return response.data;
  },
  
  getCurrentUser: async (): Promise<any> => {
    const response = await api.get('/auth/me');
    return response.data;
  }
};

export const cartApi = {
  getCart: async (): Promise<ShoppingCart> => {
    const response = await api.get('/shoppingcart');
    return response.data;
  },
  
  addToCart: async (productId: number, quantity: number): Promise<CartItem> => {
    const response = await api.post('/shoppingcart/add', { productId, quantity });
    return response.data;
  },
  
  updateCartItem: async (itemId: number, quantity: number): Promise<CartItem> => {
    const response = await api.put(`/shoppingcart/items/${itemId}`, { quantity });
    return response.data;
  },
  
  removeFromCart: async (itemId: number): Promise<void> => {
    await api.delete(`/shoppingcart/items/${itemId}`);
  },
  
  clearCart: async (): Promise<void> => {
    await api.delete('/shoppingcart');
  }
};

export const ordersApi = {
  getOrders: async (): Promise<Order[]> => {
    const response = await api.get('/orders');
    return response.data;
  },
  
  getOrder: async (id: number): Promise<Order> => {
    const response = await api.get(`/orders/${id}`);
    return response.data;
  },
  
  createOrder: async (orderData: any): Promise<Order> => {
    const response = await api.post('/orders', orderData);
    return response.data;
  }
};

export default api;