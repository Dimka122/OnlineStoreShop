import React, { useState, useEffect } from 'react';
import {
  Container,
  Typography,
  Card,
  CardMedia,
  CardContent,
  CardActions,
  Button,
  Box,
  CircularProgress,
  Alert,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Pagination,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { ShoppingCart } from '@mui/icons-material';
import { productsApi, categoriesApi, cartApi } from '../services/api';
import { Product, Category } from '../types';
import { useAuth } from '../contexts/AuthContext';

const Products: React.FC = () => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<number | ''>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [addingToCart, setAddingToCart] = useState<number | null>(null);

  const pageSize = 12;

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        
        // Fetch categories
        const categoriesDataRaw: any = await categoriesApi.getCategories();
        const resolvedCategories: Category[] = Array.isArray(categoriesDataRaw)
          ? categoriesDataRaw
          : (Array.isArray(categoriesDataRaw?.data) ? categoriesDataRaw.data : []);
        setCategories(resolvedCategories);

        // Fetch products
        const productsDataRaw: any = await productsApi.getProducts(
          currentPage,
          pageSize,
          selectedCategory || undefined,
          searchTerm || undefined
        );

        const pd = productsDataRaw?.data;
        const resolvedProducts: Product[] = Array.isArray(productsDataRaw)
          ? productsDataRaw
          : (Array.isArray(pd?.products)
              ? pd.products
              : (Array.isArray(pd?.data) ? pd.data : (Array.isArray(productsDataRaw?.items) ? productsDataRaw.items : [])));

        setProducts(resolvedProducts);
        const totalPagesComputed = typeof pd?.totalPages === 'number'
          ? pd.totalPages
          : (typeof productsDataRaw?.totalPages === 'number' ? productsDataRaw.totalPages : Math.max(1, Math.ceil(resolvedProducts.length / pageSize)));
        setTotalPages(totalPagesComputed);
      } catch (err) {
        setError('Не удалось загрузить товары');
        console.error('Error fetching data:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [currentPage, selectedCategory, searchTerm]);

  const handleSearch = (event: React.FormEvent) => {
    event.preventDefault();
    setCurrentPage(1);
  };

  const handleCategoryChange = (event: any) => {
    setSelectedCategory(event.target.value);
    setCurrentPage(1);
  };

  const handleProductClick = (productId: number) => {
    navigate(`/products/${productId}`);
  };

  const handleAddToCart = async (productId: number) => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    try {
      setAddingToCart(productId);
      await cartApi.addToCart(productId, 1);
      // You could show a success message here
    } catch (error) {
      console.error('Error adding to cart:', error);
      // You could show an error message here
    } finally {
      setAddingToCart(null);
    }
  };

  const formatPrice = (price: number, salePrice?: number) => {
    if (salePrice && salePrice < price) {
      return (
        <Box>
          <Typography variant="body2" color="text.secondary" sx={{ textDecoration: 'line-through' }}>
            ${price.toFixed(2)}
          </Typography>
          <Typography variant="h6" color="error">
            ${salePrice.toFixed(2)}
          </Typography>
        </Box>
      );
    }
    return <Typography variant="h6">${price.toFixed(2)}</Typography>;
  };

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, textAlign: 'center' }}>
        <CircularProgress />
      </Container>
    );
  }

  return (
    <Container maxWidth="lg">
      <Typography variant="h4" component="h1" gutterBottom>
        Товары
      </Typography>

      {/* Search and Filter */}
      <Box component="form" onSubmit={handleSearch} sx={{ mb: 4 }}>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
          <TextField
            fullWidth
            placeholder="Поиск товаров..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            sx={{ flex: 1, minWidth: 200 }}
          />
          <FormControl sx={{ minWidth: 150 }}>
            <InputLabel>Категория</InputLabel>
            <Select
              value={selectedCategory}
              onChange={handleCategoryChange}
              label="Категория"
            >
              <MenuItem value="">Все категории</MenuItem>
              {categories.map((category) => (
                <MenuItem key={category.id} value={category.id}>
                  {category.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Button type="submit" variant="contained">
            Поиск
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {products.length === 0 ? (
        <Box textAlign="center" py={4}>
          <Typography color="text.secondary">
            Товары не найдены
          </Typography>
        </Box>
      ) : (
        <>
          <Box sx={{ 
            display: 'grid', 
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)', lg: 'repeat(4, 1fr)' },
            gap: 3 
          }}>
            {products.map((product) => (
              <Card
                key={product.id}
                sx={{
                  height: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                  transition: 'transform 0.2s, box-shadow 0.2s',
                  '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: 4,
                  },
                }}
              >
                <CardMedia
                  component="img"
                  height="200"
                  image={product.imageUrl || `https://picsum.photos/seed/${product.id}/400/300.jpg`}
                  alt={product.name}
                  sx={{ objectFit: 'cover' }}
                />
                <CardContent sx={{ flexGrow: 1 }}>
                  <Typography variant="h6" component="h3" gutterBottom>
                    {product.name}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    {product.description.length > 80
                      ? `${product.description.substring(0, 80)}...`
                      : product.description}
                  </Typography>
                  {formatPrice(product.price, product.salePrice)}
                  <Box sx={{ display: 'flex', alignItems: 'center', mt: 1 }}>
                    <Typography variant="body2" color="text.secondary">
                      ⭐ {product.averageRating.toFixed(1)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ ml: 2 }}>
                      ({product.reviews.length} отзывов)
                    </Typography>
                  </Box>
                </CardContent>
                <CardActions sx={{ p: 2, pt: 0 }}>
                  <Button
                    size="small"
                    color="primary"
                    onClick={() => handleProductClick(product.id)}
                  >
                    Подробнее
                  </Button>
                  <Button
                    size="small"
                    color="secondary"
                    startIcon={<ShoppingCart />}
                    onClick={() => handleAddToCart(product.id)}
                    disabled={addingToCart === product.id || product.stockQuantity === 0}
                  >
                    {addingToCart === product.id ? 'Добавление...' : 'В корзину'}
                  </Button>
                </CardActions>
              </Card>
            ))}
          </Box>

          {/* Pagination */}
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <Pagination
              count={totalPages}
              page={currentPage}
              onChange={(event, value) => setCurrentPage(value)}
              color="primary"
            />
          </Box>
        </>
      )}
    </Container>
  );
};

export default Products;