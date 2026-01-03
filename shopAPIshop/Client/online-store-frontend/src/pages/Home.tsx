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
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { productsApi } from '../services/api';
import { Product } from '../types';

const Home: React.FC = () => {
  const navigate = useNavigate();
  const [featuredProducts, setFeaturedProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchFeaturedProducts = async () => {
      try {
        const products = await productsApi.getFeaturedProducts();
        setFeaturedProducts(products);
      } catch (err) {
        setError('Не удалось загрузить рекомендуемые товары');
        console.error('Error fetching featured products:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchFeaturedProducts();
  }, []);

  const handleProductClick = (productId: number) => {
    navigate(`/products/${productId}`);
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
      {/* Hero Section */}
      <Box
        sx={{
          bgcolor: 'primary.main',
          color: 'primary.contrastText',
          p: 8,
          borderRadius: 2,
          mb: 6,
          textAlign: 'center',
        }}
      >
        <Typography variant="h3" component="h1" gutterBottom>
          Добро пожаловать в наш магазин!
        </Typography>
        <Typography variant="h6" paragraph>
          Откройте для себя удивительные товары по отличным ценам
        </Typography>
        <Button
          variant="contained"
          color="secondary"
          size="large"
          onClick={() => navigate('/products')}
        >
          Перейти к товарам
        </Button>
      </Box>

      {/* Featured Products */}
      <Box sx={{ mb: 6 }}>
        <Typography variant="h4" component="h2" gutterBottom textAlign="center">
          Рекомендуемые товары
        </Typography>
        
        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}

        {featuredProducts.length === 0 ? (
          <Typography textAlign="center" color="text.secondary">
            Рекомендуемые товары暂时 отсутствуют
          </Typography>
        ) : (
          <Box sx={{ 
            display: 'grid', 
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)', lg: 'repeat(4, 1fr)' },
            gap: 3 
          }}>
            {featuredProducts.map((product) => (
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
                    {product.description.length > 100
                      ? `${product.description.substring(0, 100)}...`
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
                <CardActions>
                  <Button
                    size="small"
                    color="primary"
                    onClick={() => handleProductClick(product.id)}
                  >
                    Подробнее
                  </Button>
                </CardActions>
              </Card>
            ))}
          </Box>
        )}
      </Box>

      {/* Features Section */}
      <Box sx={{ py: 6, textAlign: 'center' }}>
        <Typography variant="h4" component="h2" gutterBottom>
          Почему выбирают нас
        </Typography>
        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" }, gap: 4, mt: 2 }}>
          <Box>
            <Typography variant="h6" gutterBottom>
              🚚 Быстрая доставка
            </Typography>
            <Typography color="text.secondary">
              Доставка по всей стране за 1-3 дня
            </Typography>
          </Box>
          <Box>
            <Typography variant="h6" gutterBottom>
              💰 Лучшие цены
            </Typography>
            <Typography color="text.secondary">
              Гарантия лучшей цены на рынке
            </Typography>
          </Box>
          <Box>
            <Typography variant="h6" gutterBottom>
              🛡️ Безопасная оплата
            </Typography>
            <Typography color="text.secondary">
              100% безопасность ваших платежей
            </Typography>
          </Box>
        </Box>
      </Box>
    </Container>
  );
};

export default Home;