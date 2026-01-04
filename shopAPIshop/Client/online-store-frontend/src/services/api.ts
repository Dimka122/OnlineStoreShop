import axios from 'axios';
import { Product, Category, CartItem, ShoppingCart, LoginRequest, RegisterRequest, AuthResponse, Order } from '../types';

const API_BASE_URL = 'https://localhost:54131/api';
const API_ORIGIN = API_BASE_URL.replace(/\/api$/i, '');

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
    const raw = response.data;
    const outer = raw?.data ?? raw?.Data ?? raw;

    // Try to locate products array in common shapes
    const productsArray = outer?.Products ?? outer?.products ?? outer?.data?.Products ?? outer?.data ?? outer;

    const list = Array.isArray(productsArray) ? productsArray : [];

    return list.map((p: any) => ({ ...p, imageUrl: toAbsoluteUrl(p.imageUrl ?? p.ImageUrl) }));
  },
  
  getProduct: async (id: number): Promise<Product> => {
    const response = await api.get(`/products/${id}`);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    const data = payload?.Data ?? payload ?? response.data;
    // ensure image url absolute
    if (data?.ImageUrl) data.ImageUrl = toAbsoluteUrl(data.ImageUrl);
    return data;
  },
  
  getFeaturedProducts: async (count = 4): Promise<Product[]> => {
    const params: any = { page: 1, pageSize: count, isFeatured: true };
    const response = await api.get('/products', { params });
    // Normalize different response shapes
    const data = response.data;
    const list = Array.isArray(data) ? data : data?.products ?? data?.data ?? [];
    return (list || []).map((p: any) => ({ ...p, imageUrl: toAbsoluteUrl(p.imageUrl ?? p.ImageUrl) })) as Product[];
  },
  
  createReview: async (productId: number, review: { rating: number; comment: string }) => {
    // ReviewsController expects POST /api/reviews?productId={id}
    const response = await api.post(`/reviews?productId=${productId}`, review);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return payload?.Data ?? payload;
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
    // Ensure we don't send 'application/json' Content-Type from default headers
    const config: any = { headers: { ...(api.defaults.headers.common || {}) } };
    if (config.headers['Content-Type']) delete config.headers['Content-Type'];
    const response = await api.post('/products', formData, config);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return payload?.Data ?? payload;
  },

  updateProductWithImage: async (id: number, formData: FormData) => {
    const config: any = { headers: { ...(api.defaults.headers.common || {}) } };
    if (config.headers['Content-Type']) delete config.headers['Content-Type'];
    const response = await api.put(`/products/${id}`, formData, config);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return payload?.Data ?? payload;
  }
};

export const categoriesApi = {
  getCategories: async (): Promise<Category[]> => {
    const response = await api.get('/categories');
    // backend may wrap response into { Message, Data } or { data }
    return response.data?.data ?? response.data?.Data ?? response.data;
  }
  ,
  createCategory: async (category: Partial<Category>) => {
    const response = await api.post('/categories', category);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return payload?.Data ?? payload;
  },
  updateCategory: async (id: number, category: Partial<Category>) => {
    const response = await api.put(`/categories/${id}`, category);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return payload?.Data ?? payload;
  },
  deleteCategory: async (id: number) => {
    const response = await api.delete(`/categories/${id}`);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return payload?.Data ?? payload;
  }
};

export const authApi = {
  login: async (credentials: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post('/auth/login', credentials);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    // payload expected to be AuthResponseDTO { Token, Expiration, User }
    const token = payload?.Token ?? payload?.token ?? null;
    const userDto = payload?.User ?? payload?.user ?? payload;
    const user = mapUserDtoToUser(userDto);
    return { token, user } as AuthResponse;
  },
  
  register: async (userData: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post('/auth/register', userData);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    const token = payload?.Token ?? payload?.token ?? null;
    const userDto = payload?.User ?? payload?.user ?? payload;
    const user = mapUserDtoToUser(userDto);
    return { token, user } as AuthResponse;
  },
  
  getCurrentUser: async (): Promise<any> => {
    const response = await api.get('/auth/me');
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    // payload is user DTO
    return mapUserDtoToUser(payload);
  }
};

export const cartApi = {
  getCart: async (): Promise<ShoppingCart> => {
    const response = await api.get('/shoppingcart');
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return mapShoppingCartDtoToShoppingCart(payload);
  },
  
  addToCart: async (productId: number, quantity: number): Promise<ShoppingCart> => {
    const response = await api.post('/shoppingcart/add', { productId, quantity });
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return mapShoppingCartDtoToShoppingCart(payload);
  },
  
  updateCartItem: async (itemId: number, quantity: number): Promise<ShoppingCart> => {
    const response = await api.put(`/shoppingcart/items/${itemId}`, { quantity });
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    return mapShoppingCartDtoToShoppingCart(payload);
  },
  
  removeFromCart: async (itemId: number): Promise<ShoppingCart | void> => {
    const response = await api.delete(`/shoppingcart/items/${itemId}`);
    const payload = response.data?.data ?? response.data?.Data ?? response.data;
    // some endpoints return updated cart
    if (payload) return mapShoppingCartDtoToShoppingCart(payload);
    return undefined;
  },
  
  clearCart: async (): Promise<void> => {
    // backend clear endpoint is DELETE /shoppingcart/clear
    const response = await api.delete('/shoppingcart/clear');
    return (response.data as any) ?? undefined;
  }
};

// Helpers to map backend DTOs to frontend types
function mapUserDtoToUser(userDto: any): any {
  if (!userDto) return null;
  const roles: string[] = userDto?.Roles ?? userDto?.roles ?? [];
  const isAdmin = Array.isArray(roles) && roles.map(r => r.toLowerCase()).includes('admin');
  return {
    id: userDto?.Id ?? userDto?.id,
    email: userDto?.Email ?? userDto?.email,
    firstName: userDto?.FirstName ?? userDto?.firstName ?? '',
    lastName: userDto?.LastName ?? userDto?.lastName ?? '',
    role: isAdmin ? 'admin' : 'user',
  };
}

function mapShoppingCartDtoToShoppingCart(dto: any): ShoppingCart {
  if (!dto) return { id: 0, items: [], totalAmount: 0 };
  const items: CartItem[] = (dto.CartItems ?? dto.cartItems ?? dto.items ?? []).map((ci: any) => ({
    id: ci.Id ?? ci.id,
    product: {
      id: ci.ProductId ?? ci.productId ?? 0,
      name: ci.ProductName ?? ci.productName ?? '',
      description: ci.ProductDescription ?? '',
      price: ci.UnitPrice ?? ci.unitPrice ?? 0,
      salePrice: ci.SalePrice ?? ci.salePrice ?? undefined,
      stockQuantity: ci.AvailableStock ?? ci.availableStock ?? 0,
      imageUrl: toAbsoluteUrl(ci.ProductImageUrl ?? ci.productImageUrl ?? ci.product?.imageUrl ?? undefined),
      isActive: true,
      isFeatured: false,
      createdAt: (dto.CreatedAt ?? new Date()).toString(),
      updatedAt: undefined,
      category: { id: 0, name: ci.CategoryName ?? ci.categoryName ?? (ci.product?.category?.name ?? '') },
      reviews: [],
      averageRating: 0,
    },
    quantity: ci.Quantity ?? ci.quantity ?? 1,
    price: (ci.CurrentPrice ?? ci.currentPrice ?? ci.UnitPrice ?? ci.unitPrice ?? 0)
  } as CartItem));

  return {
    id: dto.Id ?? dto.id ?? 0,
    items,
    totalAmount: dto.Total ?? dto.total ?? items.reduce((s: number, it: CartItem) => s + (it.price * it.quantity), 0)
  };
}

function toAbsoluteUrl(url: string | undefined | null): string | undefined {
  if (!url) return undefined;
  // If already absolute, return as is
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  // If starts with '/', prefix backend origin
  if (url.startsWith('/')) return `${API_ORIGIN}${url}`;
  return url;
}

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