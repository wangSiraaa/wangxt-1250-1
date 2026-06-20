import axios, { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import { ElMessage } from 'element-plus'

const service: AxiosInstance = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

service.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    return config
  },
  (error) => {
    console.error('请求错误:', error)
    return Promise.reject(error)
  }
)

service.interceptors.response.use(
  (response: AxiosResponse) => {
    return response
  },
  (error) => {
    console.error('响应错误:', error)
    if (error.response) {
      const status = error.response.status
      const data = error.response.data
      let message = '请求失败'
      
      if (data?.error) {
        message = data.error
      } else if (typeof data === 'string') {
        message = data
      } else if (status === 404) {
        message = '请求的资源不存在'
      } else if (status === 500) {
        message = '服务器内部错误'
      } else if (status === 400) {
        message = '请求参数错误'
      }
      
      ElMessage.error(message)
    } else if (error.message?.includes('timeout')) {
      ElMessage.error('请求超时，请稍后重试')
    } else {
      ElMessage.error('网络错误，请检查连接')
    }
    return Promise.reject(error)
  }
)

export default service
