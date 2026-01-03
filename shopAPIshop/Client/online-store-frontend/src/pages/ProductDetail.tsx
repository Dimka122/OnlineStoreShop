import React, { useState, useEffect } from 'react';
import {
  Container,
  Grid,
  Card,
  CardMedia,
  Typography,
  Button,
  Box,
  CircularProgress,
  Alert,
  Rating,
  TextField,
  List,
  ListItem,
  ListItemText,
  Divider,
  Chip,
} from '@mui/material';
import { useParams, useNavigate } from 'react-router-dom';
import { ShoppingCart, ArrowBack } from '@mui/icons-material';
import { productsApi, cartApi } from '../services/api';
import { Product, ProductReview } from '../types';
import { useAuth } from '../contexts/AuthContext';

const ProductDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [product, setProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [addingToCart, setAddingToCart] = useState(false);
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewComment, setReviewComment] = useState('');
  const [submittingReview, setSubmittingReview] = useState(false);

  useEffect(() => {
    const fetchProduct = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const productData = await productsApi.getProduct(parseInt(id));
        setProduct(productData);
      } catch (err) {
        setError('Не удалось загрузить товар');
        console.error('Error fetching product:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchProduct();
  }, [id]);

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    if (!product) return;

    try {
      setAddingToCart(true);
      await cartApi.addToCart(product.id, quantity);
      // You could show a success message here
    } catch (error) {
      console.error('Error adding to cart:', error);
      // You could show an error message here
    } finally {
      setAddingToCart(false);
    }
  };

  const handleSubmitReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!product || !isAuthenticated) return;

    try {
      setSubmittingReview(true);
      await productsApi.createReview(product.id, {
        rating: reviewRating,
        comment: reviewComment,
      });
      
      // Refresh product data to show new review
      const updatedProduct = await productsApi.getProduct(product.id);
      setProduct(updatedProduct);
      
      // Reset form
      setReviewComment('');
      setReviewRating(5);
    } catch (error) {
      console.error('Error submitting review:', error);
      // You could show an error message here
    } finally {
      setSubmittingReview(false);
    }
  };

  const formatPrice = (price: number, salePrice?: number) => {
    if (typeof price !== 'number' || Number.isNaN(price)) {
      return <Typography variant="h6">Цена недоступна</Typography>;
    }

    if (typeof salePrice === 'number' && !Number.isNaN(salePrice) && salePrice < price) {
      return (
        <Box>
          <Typography variant="h6" color="text.secondary" sx={{ textDecoration: 'line-through' }}>
            ${price.toFixed(2)}
          </Typography>
          <Typography variant="h4" color="error">
            ${salePrice.toFixed(2)}
          </Typography>
        </Box>
      );
    }

    return <Typography variant="h4">${price.toFixed(2)}</Typography>;
  };

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, textAlign: 'center' }}>
        <CircularProgress />
      </Container>
    );
  }

  if (error || !product) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4 }}>
        <Alert severity="error">{error || 'Товар не найден'}</Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg">
      <Button
        startIcon={<ArrowBack />}
        onClick={() => navigate(-1)}
        sx={{ mb: 2 }}
      >
        Назад
      </Button>

      <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
        {/* Product Image */}
        <Box sx={{ flex: 1, minWidth: 300 }}>
          <Card>
            <CardMedia
              component="img"
              image={product.imageUrl || `https://picsum.photos/seed/${product.id}/600/600.jpg`}
              alt={product.name}
              sx={{ height: 600, objectFit: 'cover' }}
            />
          </Card>
        </Box>

        {/* Product Info */}
        <Box sx={{ flex: 1, minWidth: 300 }}>
          <Typography variant="h4" component="h1" gutterBottom>
            {product.name}
          </Typography>

          <Box sx={{ mb: 2 }}>
            <Rating value={product.averageRating ?? 0} precision={0.1} readOnly />
            <Typography variant="body2" color="text.secondary" sx={{ ml: 1 }}>
              ({product.reviews?.length ?? 0} отзывов)
            </Typography>
          </Box>

          {formatPrice(product.price, product.salePrice)}

          <Typography variant="body1" sx={{ my: 3 }}>
            {product.description || ''}
          </Typography>

          <Box sx={{ mb: 3 }}>
            <Typography variant="body2" color="text.secondary">
              Категория: {product.category?.name || ''}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              В наличии: {product.stockQuantity} шт.
            </Typography>
            {product.isFeatured && (
              <Chip label="Рекомендуемый" color="primary" size="small" sx={{ mt: 1 }} />
            )}
          </Box>

          {/* Quantity and Add to Cart */}
          <Box sx={{ mb: 4 }}>
            <Typography variant="h6" gutterBottom>
              Количество:
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <TextField
                type="number"
                value={quantity}
                onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
                inputProps={{ min: 1, max: product.stockQuantity }}
                sx={{ width: 100 }}
              />
              <Button
                variant="contained"
                size="large"
                startIcon={<ShoppingCart />}
                onClick={handleAddToCart}
                disabled={addingToCart || product.stockQuantity === 0}
              >
                {addingToCart ? 'Добавление...' : 'В корзину'}
              </Button>
            </Box>
            {product.stockQuantity === 0 && (
              <Typography color="error" sx={{ mt: 1 }}>
                Товар временно отсутствует
              </Typography>
            )}
          </Box>
        </Box>
      </Box>

      {/* Reviews Section */}
      <Box sx={{ mt: 6 }}>
        <Typography variant="h4" component="h2" gutterBottom>
          Отзывы
        </Typography>

        {/* Add Review Form */}
        {isAuthenticated && (
          <Box component="form" onSubmit={handleSubmitReview} sx={{ mb: 4 }}>
            <Typography variant="h6" gutterBottom>
              Оставить отзыв
            </Typography>
            <Box sx={{ mb: 2 }}>
              <Rating
                value={reviewRating}
                onChange={(_, newValue) => setReviewRating(newValue || 5)}
              />
            </Box>
            <TextField
              fullWidth
              multiline
              rows={3}
              placeholder="Ваш отзыв..."
              value={reviewComment}
              onChange={(e) => setReviewComment(e.target.value)}
              required
              sx={{ mb: 2 }}
            />
            <Button
              type="submit"
              variant="contained"
              disabled={submittingReview}
            >
              {submittingReview ? 'Отправка...' : 'Отправить отзыв'}
            </Button>
          </Box>
        )}

        {/* Reviews List */}
        {(product.reviews?.length ?? 0) === 0 ? (
          <Typography color="text.secondary">
            Пока нет отзывов. Будьте первым!
          </Typography>
        ) : (
          <List>
            {(product.reviews || []).map((review) => (
              <React.Fragment key={review.id}>
                <ListItem alignItems="flex-start">
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                        <Typography variant="subtitle1">
                          {review.user.firstName} {review.user.lastName}
                        </Typography>
                        <Rating value={review.rating} size="small" readOnly />
                      </Box>
                    }
                    secondary={
                      <>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(review.createdAt).toLocaleDateString()}
                        </Typography>
                        <Typography variant="body1" sx={{ mt: 1 }}>
                          {review.comment}
                        </Typography>
                      </>
                    }
                  />
                </ListItem>
                <Divider variant="inset" component="li" />
              </React.Fragment>
            ))}
          </List>
        )}
      </Box>
    </Container>
  );
};

export default ProductDetail;